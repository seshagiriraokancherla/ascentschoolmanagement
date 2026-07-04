import Foundation
import Observation

@Observable
final class AuthViewModel {

    enum Phase {
        case mobileEntry
        case otpEntry(mobile: String)
        case childSelector(children: [ChildDto])

        var caseKey: Int {
            switch self {
            case .mobileEntry:    return 0
            case .otpEntry:       return 1
            case .childSelector:  return 2
            }
        }
    }

    // MARK: - Inputs

    var phase: Phase = .mobileEntry
    var mobile: String = ""
    var otp: String = ""

    // MARK: - Async state

    var isLoading: Bool = false
    var errorMessage: String?

    var resendCountdown: Int = 0
    private var resendTask: Task<Void, Never>?

    // MARK: - Parent OTP flow

    func requestOtp() async {
        let normalized = mobile.filter { $0.isNumber }
        guard normalized.count == 10 else {
            errorMessage = "Enter your 10-digit mobile number."
            return
        }
        mobile = normalized
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            try await APIClient.shared.requestParentOtp(
                mobile: normalized,
                deviceId: DeviceIDProvider.deviceId
            )
            otp = ""
            phase = .otpEntry(mobile: normalized)
            startResendCountdown()
        } catch {
            errorMessage = message(for: error)
        }
    }

    func verifyOtp() async {
        guard case .otpEntry(let confirmedMobile) = phase else { return }
        let code = otp.filter { $0.isNumber }
        guard code.count == 6 else {
            errorMessage = "Enter the 6-digit code."
            return
        }
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            let auth = try await APIClient.shared.verifyParentOtp(
                mobile: confirmedMobile,
                otp: code,
                deviceId: DeviceIDProvider.deviceId
            )
            // IMPORTANT: use the "without child" save. The verify-otp response
            // body may include studentId (server returns last-selected child for
            // convenience) but the JWT itself has NO child claims. Saving studentId
            // would flip `isFullyAuthenticated` to true and RootView would route
            // past the child selector, leaving the user on home with a parent-only
            // token — every /mobile/student/* call then 401s with
            // "Please select a child first.".
            KeychainTokenStore.shared.saveParentAuthWithoutChild(auth)
            cancelResendCountdown()
            await loadChildren()
        } catch {
            errorMessage = message(for: error)
        }
    }

    func loadChildren() async {
        isLoading = true
        defer { isLoading = false }

        do {
            let children = try await APIClient.shared.parentChildren()
            if children.isEmpty {
                errorMessage = "No children are linked to this number for \(AppInfo.displayName) yet. Please check with the school office."
                return
            }
            phase = .childSelector(children: children)
        } catch {
            errorMessage = message(for: error)
        }
    }

    func selectChild(linkId: Int) async {
        isLoading = true
        defer { isLoading = false }
        do {
            let auth = try await APIClient.shared.selectChild(linkId: linkId)
            // Save again — this token has child context (studentId, className, …).
            // RootView observes `isFullyAuthenticated` and routes to home automatically.
            KeychainTokenStore.shared.saveParentAuth(auth)
            // Phase 68: also persist the link_id so we can silently re-select this
            // child after any parent refresh (cold start / mid-session 401).
            KeychainTokenStore.shared.saveChildLinkId(linkId)
        } catch {
            errorMessage = message(for: error)
        }
    }

    // MARK: - Teacher login

    func loginTeacher(username: String, password: String) async {
        let trimmedUser = username.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedUser.isEmpty, !password.isEmpty else {
            errorMessage = "Enter your username and password."
            return
        }
        isLoading = true
        defer { isLoading = false }
        do {
            let auth = try await APIClient.shared.loginTeacher(
                TeacherLoginRequest(username: trimmedUser, password: password)
            )
            KeychainTokenStore.shared.saveTeacherAuth(auth)
        } catch {
            errorMessage = message(for: error)
        }
    }

    // MARK: - Navigation

    func backToMobile() {
        phase = .mobileEntry
        otp = ""
        errorMessage = nil
        cancelResendCountdown()
    }

    func clearError() {
        errorMessage = nil
    }

    // MARK: - Resend countdown

    private func startResendCountdown() {
        cancelResendCountdown()
        resendCountdown = 30
        resendTask = Task { [weak self] in
            while !Task.isCancelled {
                guard let self else { return }
                if self.resendCountdown <= 0 { return }
                try? await Task.sleep(for: .seconds(1))
                if Task.isCancelled { return }
                self.resendCountdown = max(0, self.resendCountdown - 1)
            }
        }
    }

    private func cancelResendCountdown() {
        resendTask?.cancel()
        resendTask = nil
        resendCountdown = 0
    }

    private func message(for error: Error) -> String {
        (error as? APIError)?.errorDescription ?? error.localizedDescription
    }
}
