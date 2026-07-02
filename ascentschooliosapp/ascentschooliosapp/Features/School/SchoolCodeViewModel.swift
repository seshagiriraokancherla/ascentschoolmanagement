import Foundation
import Observation

// Phase 44 (Android parity: SchoolCodeViewModel.kt) — three-step state machine
// for the generic-flavor school-code entry:
//     idle → resolving → confirm (school name) → done (school saved)
//   error at any point.
// On confirm we save the subdomain + branding to KeychainTokenStore; RootView
// observes the store and drops the school-code gate as soon as the code appears.
@Observable
final class SchoolCodeViewModel {

    enum State {
        case idle
        case resolving
        case confirm(SchoolByCodeDto)
        case done
        case error(String)
    }

    var state: State = .idle
    var code: String = ""

    var isBusy: Bool {
        if case .resolving = state { return true }
        return false
    }

    func resolve() async {
        let digits = code.filter { $0.isNumber }
        // Android's contract: 4 digits. Widen slightly here since the server
        // is the source of truth — but reject empty / too-short input up front.
        guard digits.count >= 3, digits.count <= 6 else {
            state = .error("Enter the school code shared by your school office.")
            return
        }
        code = digits
        state = .resolving

        do {
            let school = try await APIClient.shared.schoolByCode(code: digits)
            state = .confirm(school)
        } catch {
            state = .error(errorMessage(for: error))
        }
    }

    func confirm(_ school: SchoolByCodeDto) async {
        state = .resolving

        // Save the school first so /branding's request carries X-School-Code /
        // X-Subdomain = <resolved subdomain>. Without this the branding endpoint
        // has no way to pick the right tenant.
        KeychainTokenStore.shared.saveSchool(
            code: school.schoolCode,
            name: school.name,
            logoUrl: nil
        )

        // Branding is best-effort — school code + name alone are enough to
        // proceed. If the fetch fails we still let the user through and
        // /branding can be retried later.
        do {
            let branding = try await APIClient.shared.branding()
            KeychainTokenStore.shared.updateBranding(
                name: branding.displayName ?? school.name,
                logoUrl: absoluteLogoURL(branding.logoUrl)
            )
        } catch {
            // Non-fatal.
        }

        state = .done
    }

    func reject() {
        // User confirmed the school doesn't look right — reset to code entry.
        state = .idle
    }

    func clearError() {
        if case .error = state { state = .idle }
    }

    // MARK: - Helpers

    private func absoluteLogoURL(_ path: String?) -> String? {
        guard let path, !path.isEmpty else { return nil }
        // If the server returned a relative path (`/uploads/logo.png`) resolve
        // against the API host; already-absolute URLs pass through unchanged.
        if path.hasPrefix("http://") || path.hasPrefix("https://") { return path }
        return AppInfo.absoluteURL(forPath: path)?.absoluteString
    }

    private func errorMessage(for error: Error) -> String {
        (error as? APIError)?.errorDescription ?? error.localizedDescription
    }
}
