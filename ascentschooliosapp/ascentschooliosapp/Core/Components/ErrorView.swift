import SwiftUI

// Centered error placeholder with optional retry action.
// Used as the failure branch on every data screen (matches Android's Error state).
struct ErrorView: View {
    let message: String
    var onRetry: (() -> Void)? = nil

    var body: some View {
        VStack(spacing: 14) {
            Image(systemName: "exclamationmark.triangle.fill")
                .font(.system(size: 40))
                .foregroundStyle(AppTheme.Palette.late)

            Text(message)
                .font(.appBodyMedium)
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .multilineTextAlignment(.center)

            if let onRetry {
                Button("Retry", action: onRetry)
                    .buttonStyle(.borderedProminent)
                    .tint(AppTheme.Palette.navyBlue)
                    .controlSize(.regular)
            }
        }
        .padding(24)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}

#Preview {
    ErrorView(message: "Couldn't load attendance for May 2026.") {}
        .background(AppTheme.Palette.appBackground)
}
