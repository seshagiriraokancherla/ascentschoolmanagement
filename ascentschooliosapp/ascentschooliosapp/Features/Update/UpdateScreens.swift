import SwiftUI

// Phase 57 (Android parity: ForceUpdateScreen) — blocking full-screen UI shown
// when the server's `updateRequired` is true. Users cannot dismiss; the only
// action is to open the App Store. Mirrors the Android behaviour of taking
// over the whole surface (no back gesture, no tab bar).
struct ForceUpdateScreen: View {
    let message: String?
    let storeUrl: URL?

    var body: some View {
        ZStack {
            AppTheme.loginGradient.ignoresSafeArea()

            VStack(spacing: 22) {
                Image(systemName: "arrow.down.circle.fill")
                    .font(.system(size: 72))
                    .foregroundStyle(.white)
                    .shadow(color: .black.opacity(0.25), radius: 8, y: 4)

                Text("Update required")
                    .font(.appHeadlineSmall)
                    .foregroundStyle(.white)

                Text(message ?? "A new version of \(AppInfo.displayName) is required to continue. Please update from the App Store.")
                    .font(.appBodyMedium)
                    .foregroundStyle(.white.opacity(0.85))
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 24)

                Button {
                    ExternalLink.open(storeUrl)
                } label: {
                    Text("Open App Store")
                        .font(.appLabelLarge.bold())
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                        .background(AppTheme.Palette.gold, in: Capsule())
                        .foregroundStyle(.white)
                }
                .padding(.horizontal, 40)
                .padding(.top, 8)
                .disabled(storeUrl == nil)
                .opacity(storeUrl == nil ? 0.5 : 1)
            }
            .padding(.vertical, 30)
        }
        .interactiveDismissDisabled(true)
    }
}

// Phase 57 (Android parity: UpdateAvailableDialog) — dismissible soft-update
// sheet. Shown at most once per process lifetime (see `AppConfigStore.softDismissed`).
struct UpdateAvailableSheet: View {
    let message: String?
    let storeUrl: URL?
    let onDismiss: () -> Void

    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(spacing: 18) {
            Image(systemName: "arrow.up.circle.fill")
                .font(.system(size: 56))
                .foregroundStyle(AppTheme.Palette.navyBlue)
                .padding(.top, 12)

            Text("Update available")
                .font(.appTitleLarge)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            Text(message ?? "A newer version is available on the App Store.")
                .font(.appBodyMedium)
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 20)

            VStack(spacing: 10) {
                Button {
                    ExternalLink.open(storeUrl)
                    onDismiss()
                    dismiss()
                } label: {
                    Text("Update now")
                        .font(.appLabelLarge.bold())
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .background(AppTheme.Palette.navyBlue, in: Capsule())
                        .foregroundStyle(.white)
                }
                .disabled(storeUrl == nil)
                .opacity(storeUrl == nil ? 0.5 : 1)

                Button {
                    onDismiss()
                    dismiss()
                } label: {
                    Text("Later")
                        .font(.appLabelMedium)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 10)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
            }
            .padding(.horizontal, 24)
            .padding(.bottom, 20)
        }
        .presentationDetents([.height(360), .medium])
        .presentationDragIndicator(.visible)
    }
}
