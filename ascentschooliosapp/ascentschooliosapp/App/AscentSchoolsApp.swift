import SwiftUI

@main
struct AscentSchoolsApp: App {
    @Environment(\.scenePhase) private var scenePhase

    init() {
        // 1. Restore HttpOnly refresh-token cookie from disk before any request fires.
        CookiePersistence.restore()
        // 2. Ensure a device id exists from first launch (used by SMS-OTP device binding).
        _ = DeviceIDProvider.deviceId
        // 3. Let APIClient read the access token from Keychain on every request.
        APIClient.shared.tokenProvider = {
            let token = KeychainTokenStore.shared.accessToken
            DebugLogger.log(.auth, "tokenProvider invoked — \(token == nil ? "NIL token" : "len=\(token!.count)")")
            return token
        }
    }

    var body: some Scene {
        WindowGroup {
            RootView()
                // Lock to light mode — Android side ships without dark theme,
                // and the navy/gold palette wasn't designed against a dark base.
                .preferredColorScheme(.light)
                // Bound Dynamic Type so AX4/AX5 don't push card layouts out of
                // bounds (capsules, status badges, summary chips). Users above
                // the cap still benefit from system zoom + VoiceOver.
                .dynamicTypeSize(.medium ... .accessibility2)
        }
        .onChange(of: scenePhase) { _, newPhase in
            if newPhase == .background {
                // Archive cookies so the refresh token survives a cold start.
                CookiePersistence.persist()
            }
        }
    }
}
