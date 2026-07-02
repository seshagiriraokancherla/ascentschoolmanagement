import SwiftUI

// Inline "nothing to show" placeholder. Used for empty lists (no homework yet,
// no announcements this week, etc.) — matches Android's empty-state cards.
struct EmptyState: View {
    let systemImage: String
    let title: String
    var message: String? = nil

    var body: some View {
        VStack(spacing: 10) {
            Image(systemName: systemImage)
                .font(.system(size: 44))
                .foregroundStyle(AppTheme.Palette.textSecondary.opacity(0.4))

            Text(title)
                .font(.appTitleMedium)
                .foregroundStyle(AppTheme.Palette.textPrimary)

            if let message {
                Text(message)
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .multilineTextAlignment(.center)
            }
        }
        .padding(24)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}

#Preview {
    EmptyState(
        systemImage: "tray",
        title: "No homework yet",
        message: "Check back after your teacher posts the next assignment."
    )
    .background(AppTheme.Palette.appBackground)
}
