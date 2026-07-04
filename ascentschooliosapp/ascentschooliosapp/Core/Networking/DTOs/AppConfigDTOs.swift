import Foundation

// Phase 57 + 71 (Android parity): server-driven force/soft update gate.
// Endpoint: GET /mobile/app/config?platform=ios&applicationId=<bundleId>&versionCode=<int>
// Backed by `ascent_master.app_config`. Rows are keyed on
// (application_id, platform) — a `'*'` row acts as fallback.
//
// Server contract (from MobileAppConfigController):
//   - `updateRequired`  → versionCode < min_supported_version_code
//   - `updateAvailable` → versionCode < latest_version_code
//   - Both booleans are ANDed with `auto_update_enabled` (Phase 71). When the
//     flag is 0 the server always returns updateRequired=updateAvailable=false
//     regardless of the reported versionCode — clients still show the manual
//     "Update App" button in the login footer so users can self-update even
//     when auto-prompts are off.
//   - Fails open: if no row exists, the server returns updateRequired=false
//     / updateAvailable=false. The iOS client mirrors this: any request/decode
//     error is treated as "no update".
struct AppVersionStatusDto: Decodable {
    let updateRequired: Bool
    let updateAvailable: Bool
    let message: String?
    let storeUrl: String?
    // Full config echoed back so the client can show its own diagnostic UI if
    // needed (min/latest, autoUpdateEnabled). Optional — some server builds
    // may omit it and the client should still work.
    let minSupportedVersionCode: Int?
    let latestVersionCode: Int?
    let autoUpdateEnabled: Bool?
}
