import Foundation
import Observation

// Phase 57 + 71 (Android parity): in-memory holder for the most recent
// /mobile/app/config response. NOT persisted — we re-fetch on every cold start
// so the gate can react immediately when an admin flips
// `auto_update_enabled` or bumps `min_supported_version_code`.
//
// Two consumers today:
//   - `RootView` triggers the force / soft update UI.
//   - `AuthFlowView` reads `storeUrl` to enable the manual "Update App" button
//     in the login footer (always available regardless of the auto-update flag).
@Observable
final class AppConfigStore {
    static let shared = AppConfigStore()

    private(set) var status: AppVersionStatusDto?
    // Soft-update dismissal is remembered for the process lifetime only so
    // reopening the app after a background surfaces the dialog again.
    var softDismissed: Bool = false

    private init() {}

    var storeUrl: URL? {
        guard let raw = status?.storeUrl, !raw.isEmpty else { return nil }
        return URL(string: raw)
    }

    var isForced: Bool { status?.updateRequired == true }
    var isSoftAvailable: Bool { status?.updateAvailable == true && !isForced }

    func update(_ status: AppVersionStatusDto) {
        self.status = status
    }

    // Called by the "Refresh" affordance and by the update sheet's dismiss
    // action so the sheet doesn't re-appear on every state change until the
    // next cold start.
    func markSoftDismissed() {
        softDismissed = true
    }
}
