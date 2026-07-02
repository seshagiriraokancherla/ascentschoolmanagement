import Foundation
import UIKit

enum AppInfo {
    // Phase 44: `bakedSchoolCode` is what the xcconfig actually wrote into
    // Info.plist. For baked flavors (Demo/Stannsasf/…) it's the subdomain; for
    // the generic "CHAK IN" build it's empty. `schoolCode` returns the RESOLVED
    // code — baked if present, else whatever the user typed into the school-code
    // entry screen (persisted in KeychainTokenStore). Views should read
    // `schoolCode`; APIClient reads it too so headers stay correct even after
    // the parent hits "Change School".
    static let bakedSchoolCode: String = bundleString("SchoolCode", fallback: "")

    static var isGenericApp: Bool { bakedSchoolCode.isEmpty }

    static var schoolCode: String {
        if !bakedSchoolCode.isEmpty { return bakedSchoolCode }
        return KeychainTokenStore.shared.schoolCode ?? ""
    }

    static let apiBaseURL: String = bundleString("APIBaseURL", fallback: "https://edu-care.in/api/")
    // Baked flavors set CFBundleDisplayName from xcconfig. Generic flavor shows
    // the actively-selected school's branding name on the parent home (falls
    // back to the CHAK IN display name until a school is selected).
    static let displayName: String = bundleString("CFBundleDisplayName", fallback: "Ascent Schools")
    static var brandedDisplayName: String {
        if AppInfo.isGenericApp,
           let name = KeychainTokenStore.shared.brandingName,
           !name.isEmpty {
            return name
        }
        return displayName
    }
    static let version: String = bundleString("CFBundleShortVersionString", fallback: "1.0.0")

    // Phase 57/71 (Android parity): sent to /mobile/app/config so the server can
    // decide whether this build is behind the min/latest versionCode configured
    // in `ascent_master.app_config`. `applicationId` matches Android's
    // `BuildConfig.APPLICATION_ID`; `versionCode` matches Android's integer
    // `versionCode` — on iOS we use `CFBundleVersion` (build number), which is
    // required to be a monotonically-increasing integer per App Store rules.
    static let applicationId: String = Bundle.main.bundleIdentifier ?? "in.educare.app"
    static var versionCode: Int {
        guard let raw = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String,
              let value = Int(raw) else {
            return 1
        }
        return value
    }

    private static func bundleString(_ key: String, fallback: String) -> String {
        guard let value = Bundle.main.object(forInfoDictionaryKey: key) as? String,
              !value.isEmpty else {
            return fallback
        }
        return value
    }

    // Loads the per-school login screen logo from `Assets.xcassets`. The
    // convention is `LoginLogo-<schoolCode>` (e.g. `LoginLogo-demo`,
    // `LoginLogo-stannsasf`). Returns nil if the image set is missing or
    // empty so call-sites can show a SF Symbol placeholder.
    static var loginLogo: UIImage? {
        UIImage(named: "LoginLogo-\(schoolCode)")
    }

    // Resolve a possibly-relative server path (e.g. "/uploads/students/123.jpg")
    // against `apiBaseURL`. Returns nil for empty input.
    static func absoluteURL(forPath path: String?) -> URL? {
        guard let raw = path?.trimmingCharacters(in: .whitespacesAndNewlines), !raw.isEmpty else {
            return nil
        }
        if raw.hasPrefix("http://") || raw.hasPrefix("https://") {
            return URL(string: raw)
        }
        let base = apiBaseURL.hasSuffix("/") ? String(apiBaseURL.dropLast()) : apiBaseURL
        let suffix = raw.hasPrefix("/") ? raw : "/" + raw
        return URL(string: base + suffix)
    }
}
