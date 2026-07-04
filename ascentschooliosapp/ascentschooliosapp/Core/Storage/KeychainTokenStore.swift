import Foundation
import Observation

enum UserType: String {
    case parent
    case teacher
}

// Mirrors Android `TokenStore` (data/local/TokenStore.kt):
// keeps the access token plus user-context fields, all backed by Keychain.
// The refresh token is NOT stored here — it's an HttpOnly cookie owned by
// `HTTPCookieStorage.shared` and archived to disk by `CookiePersistence`.
@Observable
final class KeychainTokenStore {
    static let shared = KeychainTokenStore()

    private enum Keys {
        static let accessToken = "auth.accessToken"
        static let tokenType   = "auth.tokenType"
        static let userType    = "auth.userType"
        static let studentName = "auth.studentName"
        static let studentId   = "auth.studentId"
        static let admissionNo = "auth.admissionNo"
        static let className   = "auth.className"
        // Phase 68 (Android parity): persist the selected child's parent_children link_id
        // so we can silently re-select the child after any parent refresh (cold-start
        // silent refresh AND mid-session 401 auto-refresh). Without this, refresh
        // returns a parent-only JWT and every /mobile/student/* call 401s with
        // "Please select a child first." link_id is stable across promotions
        // (link upsert by admission_no), so re-selecting is safe.
        static let childLinkId = "auth.childLinkId"
        // Phase 44 (Android parity: TokenStore.schoolCode/brandingName/brandingLogoUrl)
        // — only used by the generic "CHAK IN" flavor. The parent picks a
        // school via the school-code screen and we cache the resolved subdomain
        // + branding here so subsequent cold starts skip the picker. Baked
        // flavors never write these fields.
        static let schoolCode      = "school.code"
        static let brandingName    = "school.brandingName"
        static let brandingLogoUrl = "school.brandingLogoUrl"
    }

    private(set) var accessToken: String?
    private(set) var tokenType: String?
    private(set) var userType: UserType?
    private(set) var studentName: String?
    private(set) var studentId: Int64?
    private(set) var admissionNo: String?
    private(set) var className: String?
    private(set) var childLinkId: Int?

    // Phase 44 — generic-flavor school selection (nil on baked flavors).
    private(set) var schoolCode: String?
    private(set) var brandingName: String?
    private(set) var brandingLogoUrl: String?

    var isLoggedIn: Bool { accessToken?.isEmpty == false }

    // True only when we have a *usable* session — teachers need just a token,
    // parents need the selected-child context (studentId) embedded in the JWT.
    // Used by RootView routing so the parent-only token issued right after
    // verify-otp doesn't accidentally route past the child selector.
    var isFullyAuthenticated: Bool {
        guard accessToken?.isEmpty == false else { return false }
        switch userType {
        case .teacher: return true
        case .parent:  return studentId != nil
        case .none:    return false
        }
    }

    private init() {
        accessToken = KeychainHelper.readString(Keys.accessToken)
        tokenType   = KeychainHelper.readString(Keys.tokenType)
        if let raw = KeychainHelper.readString(Keys.userType) {
            userType = UserType(rawValue: raw)
        }
        studentName = KeychainHelper.readString(Keys.studentName)
        if let raw = KeychainHelper.readString(Keys.studentId), let value = Int64(raw) {
            studentId = value
        }
        admissionNo = KeychainHelper.readString(Keys.admissionNo)
        className   = KeychainHelper.readString(Keys.className)
        if let raw = KeychainHelper.readString(Keys.childLinkId), let value = Int(raw) {
            childLinkId = value
        }
        schoolCode      = KeychainHelper.readString(Keys.schoolCode)
        brandingName    = KeychainHelper.readString(Keys.brandingName)
        brandingLogoUrl = KeychainHelper.readString(Keys.brandingLogoUrl)
    }

    // MARK: - Mutations

    // Used after verify-otp / parent login: stores the token + parent identity
    // but EXPLICITLY clears the child context. The server's verify-otp response
    // may include `studentId` for convenience (e.g. last-selected child), but
    // the JWT it issues does NOT carry child claims — `/mobile/student/*`
    // endpoints would reject it with "Please select a child first."
    // Routing relies on `isFullyAuthenticated` (which requires `studentId`), so
    // keeping it nil here forces the user through the child-selector screen
    // before they can reach `ParentHomeView`.
    func saveParentAuthWithoutChild(_ response: AuthResponse) {
        write(Keys.accessToken, response.accessToken)
        write(Keys.tokenType, response.tokenType)
        write(Keys.userType, UserType.parent.rawValue)
        write(Keys.studentName, response.fullName)
        write(Keys.studentId, nil)
        write(Keys.admissionNo, nil)
        write(Keys.className, nil)

        accessToken = response.accessToken
        tokenType   = response.tokenType
        userType    = .parent
        studentName = response.fullName
        studentId   = nil
        admissionNo = nil
        className   = nil
    }

    // Used after select-child: stores the token (now with child claims in the
    // JWT) plus the resolved child context (studentId / admissionNo / className).
    func saveParentAuth(_ response: AuthResponse) {
        write(Keys.accessToken, response.accessToken)
        write(Keys.tokenType, response.tokenType)
        write(Keys.userType, UserType.parent.rawValue)
        write(Keys.studentName, response.fullName)
        write(Keys.studentId, response.studentId.map(String.init))
        write(Keys.admissionNo, response.admissionNo)
        write(Keys.className, response.className)

        accessToken = response.accessToken
        tokenType   = response.tokenType
        userType    = .parent
        studentName = response.fullName
        studentId   = response.studentId
        admissionNo = response.admissionNo
        className   = response.className
    }

    func saveTeacherAuth(_ response: TeacherAuthResponse) {
        write(Keys.accessToken, response.accessToken)
        write(Keys.tokenType, response.tokenType)
        write(Keys.userType, UserType.teacher.rawValue)
        write(Keys.studentName, response.fullName)
        write(Keys.studentId, nil)
        write(Keys.admissionNo, nil)
        write(Keys.className, nil)

        accessToken = response.accessToken
        tokenType   = response.tokenType
        userType    = .teacher
        studentName = response.fullName
        studentId   = nil
        admissionNo = nil
        className   = nil
    }

    func updateAccessToken(_ token: String) {
        write(Keys.accessToken, token)
        accessToken = token
    }

    // Phase 68: called from AuthViewModel.selectChild AND the future in-app
    // "Switch Child" flow so the link_id survives across app kills and
    // access-token refreshes.
    func saveChildLinkId(_ linkId: Int) {
        write(Keys.childLinkId, String(linkId))
        childLinkId = linkId
    }

    // MARK: - Generic-flavor school selection (Phase 44)

    // Called by SchoolCodeViewModel after /mobile/auth/school-by-code resolves.
    // `code` is the subdomain returned by the server (NOT the 4-digit login_code
    // the user typed) — it's what gets sent as X-School-Code / X-Subdomain from
    // then on.
    func saveSchool(code: String, name: String?, logoUrl: String?) {
        write(Keys.schoolCode, code)
        write(Keys.brandingName, name)
        write(Keys.brandingLogoUrl, logoUrl)
        schoolCode      = code
        brandingName    = name
        brandingLogoUrl = logoUrl
    }

    // Refreshes branding without touching the school code — used when
    // /branding is re-fetched (e.g. after the school office updates their logo).
    func updateBranding(name: String?, logoUrl: String?) {
        write(Keys.brandingName, name)
        write(Keys.brandingLogoUrl, logoUrl)
        brandingName    = name
        brandingLogoUrl = logoUrl
    }

    // Wipes the persisted school selection AND session (Phase 46 "Change
    // School" flow). Baked flavors never call this — their SCHOOL_CODE is
    // compiled in, so switching schools isn't a concept for them.
    func clearSchool() {
        [Keys.schoolCode, Keys.brandingName, Keys.brandingLogoUrl]
            .forEach { KeychainHelper.delete(key: $0) }
        schoolCode      = nil
        brandingName    = nil
        brandingLogoUrl = nil
    }

    func clear() {
        // Note: device id is owned by `DeviceIDProvider` and intentionally NOT cleared
        // — matches Android's `TokenStore.clear()` preserving `device_id`.
        [Keys.accessToken, Keys.tokenType, Keys.userType,
         Keys.studentName, Keys.studentId, Keys.admissionNo, Keys.className,
         Keys.childLinkId]
            .forEach { KeychainHelper.delete(key: $0) }

        accessToken = nil
        tokenType   = nil
        userType    = nil
        studentName = nil
        studentId   = nil
        admissionNo = nil
        className   = nil
        childLinkId = nil
    }

    // MARK: - Helpers

    private func write(_ key: String, _ value: String?) {
        if let value, !value.isEmpty {
            KeychainHelper.save(value, for: key)
        } else {
            KeychainHelper.delete(key: key)
        }
    }
}
