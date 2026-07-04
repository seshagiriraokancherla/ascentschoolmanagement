import SwiftUI

struct AuthFlowView: View {
    @State private var viewModel = AuthViewModel()
    @State private var showTeacherSheet = false

    var body: some View {
        ZStack {
            AppTheme.loginGradient
                .ignoresSafeArea()

            ScrollView {
                VStack(spacing: 20) {
                    header

                    card

                    if case .mobileEntry = viewModel.phase {
                        Button {
                            showTeacherSheet = true
                        } label: {
                            HStack(spacing: 6) {
                                Image(systemName: "person.crop.circle.badge.checkmark")
                                Text("Staff Login")
                                    .font(.appLabelLarge)
                            }
                            .foregroundStyle(.white)
                            .padding(.vertical, 10)
                            .padding(.horizontal, 16)
                            .background(.white.opacity(0.15), in: Capsule())
                            .overlay(Capsule().stroke(.white.opacity(0.3), lineWidth: 1))
                        }
                    }

                    versionFooter
                }
                .padding(.horizontal, 20)
                .padding(.top, 48)
                .padding(.bottom, 32)
            }
        }
        .preferredColorScheme(.light)
        .sheet(isPresented: $showTeacherSheet) {
            TeacherLoginSheet(viewModel: viewModel)
        }
        .alert("Couldn't continue", isPresented: errorBinding) {
            Button("OK", role: .cancel) { viewModel.clearError() }
        } message: {
            Text(viewModel.errorMessage ?? "")
        }
    }

    // MARK: - Pieces

    private var header: some View {
        VStack(spacing: 10) {
            schoolMark
                .frame(width: 96, height: 96)
                .shadow(color: .black.opacity(0.2), radius: 8, y: 4)
            Text(AppInfo.brandedDisplayName)
                .font(.appHeadlineSmall)
                .foregroundStyle(.white)
        }
    }

    // Phase 44 / 48: three-tier logo lookup.
    //   1. Baked flavor: `Assets.xcassets/LoginLogo-<schoolCode>` (built-in).
    //   2. Generic flavor with resolved school: remote branding logo URL cached
    //      in KeychainTokenStore (loaded async via AsyncImage).
    //   3. Neither: SF Symbol placeholder — kept as a last-resort fallback so
    //      the header never renders empty.
    @ViewBuilder
    private var schoolMark: some View {
        if let image = AppInfo.loginLogo {
            Image(uiImage: image)
                .resizable()
                .aspectRatio(contentMode: .fit)
                .clipShape(Circle())
                .overlay(Circle().stroke(.white.opacity(0.3), lineWidth: 2))
                .accessibilityHidden(true)
        } else if AppInfo.isGenericApp,
                  let raw = KeychainTokenStore.shared.brandingLogoUrl,
                  let url = URL(string: raw) {
            AsyncImage(url: url) { phase in
                switch phase {
                case .success(let image):
                    image
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .clipShape(Circle())
                        .overlay(Circle().stroke(.white.opacity(0.3), lineWidth: 2))
                case .empty:
                    ProgressView().progressViewStyle(.circular).tint(.white)
                default:
                    fallbackMark
                }
            }
            .accessibilityHidden(true)
        } else {
            fallbackMark
        }
    }

    private var fallbackMark: some View {
        Image(systemName: "graduationcap.circle.fill")
            .resizable()
            .aspectRatio(contentMode: .fit)
            .foregroundStyle(.white)
            .accessibilityHidden(true)
    }

    private var card: some View {
        VStack(spacing: 0) {
            switch viewModel.phase {
            case .mobileEntry:
                MobileEntryView(viewModel: viewModel)
            case .otpEntry(let mobile):
                OTPEntryView(viewModel: viewModel, mobile: mobile)
            case .childSelector(let children):
                ChildSelectorView(viewModel: viewModel, children: children)
            }
        }
        .frame(maxWidth: .infinity)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 24))
        .overlay(
            RoundedRectangle(cornerRadius: 24)
                .stroke(.white.opacity(0.25), lineWidth: 1)
        )
        .shadow(color: .black.opacity(0.15), radius: 18, y: 8)
        .animation(.easeInOut(duration: 0.25), value: viewModel.phase.caseKey)
    }

    // Phase 71 (Android parity): always-visible version + build string plus a
    // manual "Update App" button. The button is available regardless of the
    // server-side `auto_update_enabled` flag so users can self-update even
    // when auto-prompts are off. `storeUrl` comes from the /mobile/app/config
    // response cached in `AppConfigStore`; if it's not yet available (cold
    // start hasn't finished the config fetch) the button is hidden rather
    // than shown as a broken tap-target.
    private var versionFooter: some View {
        VStack(spacing: 6) {
            Text("Version \(AppInfo.version) (build \(AppInfo.versionCode))")
                .font(.appLabelSmall)
                .foregroundStyle(.white.opacity(0.6))

            HStack(spacing: 10) {
                if let url = AppConfigStore.shared.storeUrl {
                    Button {
                        ExternalLink.open(url)
                    } label: {
                        footerPill("Update App", systemImage: "arrow.up.circle")
                    }
                }

                // Phase 46: on the generic build, a "Change School" pill sits
                // beside "Update App" so the parent can switch schools before
                // logging in (e.g. wrong code entered). Baked flavors hide it.
                if AppInfo.isGenericApp {
                    Button {
                        KeychainTokenStore.shared.clearSchool()
                    } label: {
                        footerPill("Change School", systemImage: "building.columns")
                    }
                }
            }
        }
        .padding(.top, 6)
    }

    private func footerPill(_ title: String, systemImage: String) -> some View {
        HStack(spacing: 4) {
            Image(systemName: systemImage)
                .font(.system(size: 11))
            Text(title)
                .font(.appLabelSmall.bold())
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 6)
        .background(.white.opacity(0.15), in: Capsule())
        .foregroundStyle(.white)
        .overlay(Capsule().stroke(.white.opacity(0.3), lineWidth: 1))
    }

    private var errorBinding: Binding<Bool> {
        Binding(
            get: { viewModel.errorMessage != nil },
            set: { if !$0 { viewModel.clearError() } }
        )
    }
}

#Preview {
    AuthFlowView()
}
