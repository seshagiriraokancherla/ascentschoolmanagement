import SwiftUI

// Smart router with local-first session (Phase 97 parity) and post-auth routing
// to ParentHomeView (iOS-7) or TeacherHomeView (iOS-10). Launch does ZERO
// blocking auth work: if a session is stored we route straight to home from the
// long-lived stored token (which already carries child context). Token refresh
// and the version check run in the background and NEVER clear the session on a
// network failure — only an explicit 401/403 on a live call does (in APIClient).
struct RootView: View {

    private enum LoadState {
        case checking
        case ready
    }

    @State private var loadState: LoadState = .checking
    // Phase 57/71: soft-update sheet visibility is view-local so dismissing it
    // doesn't tear down the underlying auth/home surface. `AppConfigStore` still
    // tracks the "dismissed for this process" flag so we don't reopen it after
    // every state change.
    @State private var showSoftUpdate: Bool = false

    private var store: KeychainTokenStore { KeychainTokenStore.shared }
    private var appConfig: AppConfigStore { AppConfigStore.shared }

    // Phase 44: generic flavor + no persisted school code = show the school
    // picker. Baked flavors have `AppInfo.bakedSchoolCode` non-empty and are
    // never gated. Once `store.schoolCode` is set (via SchoolCodeViewModel or
    // a fresh install of a baked flavor), `AppInfo.schoolCode` starts
    // returning it and this gate resolves automatically.
    private var needsSchoolSelection: Bool {
        AppInfo.isGenericApp && (store.schoolCode ?? "").isEmpty
    }

    var body: some View {
        Group {
            switch loadState {
            case .checking:
                splash
            case .ready:
                // Phase 57: forced update is a full-screen takeover — nothing
                // else on the surface until the user updates. Placed above the
                // normal auth/home branching so it wins over anything else.
                if appConfig.isForced {
                    ForceUpdateScreen(
                        message: appConfig.status?.message,
                        storeUrl: appConfig.storeUrl
                    )
                } else if needsSchoolSelection {
                    // Phase 44: generic "CHAK IN" flavor with no school picked
                    // yet — parent must enter the 4-digit code before anything
                    // else. Baked flavors skip this gate entirely.
                    SchoolCodeView()
                } else if store.isFullyAuthenticated {
                    switch store.userType {
                    case .parent:  ParentHomeView()
                    case .teacher: TeacherHomeView()
                    case .none:    AuthFlowView()   // inconsistent — fall back
                    }
                } else {
                    AuthFlowView()
                }
            }
        }
        .task {
            // Local-first (Phase 97): route immediately from the stored session —
            // no spinner, no blocking network call that a Doze-woken phone with
            // no Wi-Fi could hang on and mistake for a dead session. The forced-
            // update screen still appears reactively (body reads `appConfig.isForced`,
            // which is @Observable) once the background check completes.
            loadState = .ready
            await checkAppVersion()
            if appConfig.isSoftAvailable && !appConfig.softDismissed {
                showSoftUpdate = true
            }
            await opportunisticRefresh()
        }
        .sheet(isPresented: $showSoftUpdate) {
            UpdateAvailableSheet(
                message: appConfig.status?.message,
                storeUrl: appConfig.storeUrl,
                onDismiss: { appConfig.markSoftDismissed() }
            )
        }
    }

    private var splash: some View {
        ZStack {
            AppTheme.loginGradient
                .ignoresSafeArea()

            VStack(spacing: 14) {
                Image(systemName: "graduationcap.circle.fill")
                    .font(.system(size: 72))
                    .foregroundStyle(.white)
                    .shadow(color: .black.opacity(0.25), radius: 8, y: 4)
                Text(AppInfo.displayName)
                    .font(.appHeadlineSmall)
                    .foregroundStyle(.white)
                ProgressView()
                    .progressViewStyle(.circular)
                    .tint(.white)
                    .padding(.top, 12)
            }
        }
        .preferredColorScheme(.light)
    }

    // Phase 97 (Android parity): OPPORTUNISTIC background refresh — fire-and-forget,
    // purely to slide the server's 365-day refresh window. It NEVER clears the
    // session: every failure path is swallowed. Contrast with the old cold-start
    // refresh, which cleared on any error and logged users out every morning on a
    // transient network hiccup. Genuine dead sessions are still caught later, by
    // the first live API call's 401 → APIClient's explicit-rejection clear.
    private func opportunisticRefresh() async {
        guard store.isLoggedIn else { return }

        switch store.userType {
        case .parent:
            // `try?` — a 401 on the refresh endpoint throws .unauthorized WITHOUT
            // clearing (refresh paths skip the retry-and-clear branch), and any
            // network error just throws; either way we return quietly.
            guard let auth = try? await APIClient.shared.refreshParent() else { return }
            let previousToken = store.accessToken
            store.updateAccessToken(auth.accessToken)
            store.updateRefreshToken(auth.refreshToken)

            // Re-establish child context (refresh returns a parent-only JWT).
            if let linkId = store.childLinkId {
                do {
                    let childAuth = try await APIClient.shared.selectChild(linkId: linkId)
                    store.saveParentAuth(childAuth)
                } catch {
                    // Roll back to the prior child-context token — committing the
                    // parent-only token would break every data screen with
                    // "Please select a child first." Never clears the session.
                    if let previousToken { store.updateAccessToken(previousToken) }
                }
            }
        case .teacher:
            guard let auth = try? await APIClient.shared.refreshTeacher() else { return }
            store.saveTeacherAuth(auth)
        case .none:
            return
        }
    }

    // Phase 57 + 71: hit /mobile/app/config on cold start (fail-open). The fetch
    // itself now lives on AppConfigStore so the home-screen global Refresh button
    // (Phase 65 pt3) can re-run the same check mid-session.
    private func checkAppVersion() async {
        await appConfig.refresh()
    }
}

#Preview {
    RootView()
}
