import SwiftUI

// Smart router with cold-start silent refresh (Phase iOS-6) and post-auth
// routing to ParentHomeView (iOS-7) or TeacherHomeView (iOS-10).
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
            await performSilentRefreshIfNeeded()
            await checkAppVersion()
            loadState = .ready
        }
        .sheet(isPresented: $showSoftUpdate) {
            UpdateAvailableSheet(
                message: appConfig.status?.message,
                storeUrl: appConfig.storeUrl,
                onDismiss: { appConfig.markSoftDismissed() }
            )
        }
        // Surface the sheet after `loadState` flips to ready — presenting it
        // during the splash caused visual jitter in local testing.
        .onChange(of: loadState) { _, newValue in
            guard newValue == .ready else { return }
            if appConfig.isSoftAvailable && !appConfig.softDismissed {
                showSoftUpdate = true
            }
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

    private func performSilentRefreshIfNeeded() async {
        // No stored token → straight to the auth flow.
        guard store.isLoggedIn else { return }

        do {
            switch store.userType {
            case .parent:
                let auth = try await APIClient.shared.refreshParent()
                // Refresh returns a parent-only JWT (no child claims — the refresh
                // cookie is parent-level, by design). Just save the new access token
                // for now; we re-establish child context below.
                store.updateAccessToken(auth.accessToken)

                // Phase 68 (Android parity): re-select the previously-selected child
                // so /mobile/student/* calls don't 401 with "Please select a child
                // first." link_id is stable across promotions (link upsert by
                // admission_no), so re-selecting is always safe. If it fails (child
                // unlinked from school, network hiccup) we fall through to
                // `isFullyAuthenticated == false` and RootView routes to the child
                // selector rather than force-logging-out.
                if let linkId = store.childLinkId {
                    do {
                        let childAuth = try await APIClient.shared.selectChild(linkId: linkId)
                        store.saveParentAuth(childAuth)
                        // linkId itself is unchanged; no need to re-save it.
                    } catch {
                        // Non-fatal — user will hit the child selector.
                    }
                }
            case .teacher:
                let auth = try await APIClient.shared.refreshTeacher()
                store.saveTeacherAuth(auth)
            case .none:
                // Inconsistent: had a token but no userType. Reset.
                store.clear()
                CookiePersistence.clear()
            }
        } catch {
            // Refresh cookie expired or network failure — force re-authentication.
            store.clear()
            CookiePersistence.clear()
        }
    }

    // Phase 57 + 71: hit /mobile/app/config on every cold start. Fails open —
    // any error (network, decode, server 500) leaves `AppConfigStore.status` as
    // nil, which means neither the force screen nor the soft sheet shows.
    private func checkAppVersion() async {
        do {
            let status = try await APIClient.shared.appConfig(
                applicationId: AppInfo.applicationId,
                versionCode: AppInfo.versionCode
            )
            appConfig.update(status)
        } catch {
            // Silently ignore — mirrors Android's fail-open behaviour so a
            // temporary API outage never blocks the app.
        }
    }
}

#Preview {
    RootView()
}
