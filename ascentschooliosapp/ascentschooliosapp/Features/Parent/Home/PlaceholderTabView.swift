import SwiftUI

// Placeholder used inside `ParentHomeView` for tabs whose feature work
// is scheduled for a later phase. Once that phase lands, swap the
// `PlaceholderTabView(...)` for the real screen and delete the call site.
struct PlaceholderTabView: View {
    let systemImage: String
    let title: String
    let phaseNumber: String

    var body: some View {
        VStack(spacing: 12) {
            Image(systemName: systemImage)
                .font(.system(size: 56))
                .foregroundStyle(AppTheme.Palette.navyBlue.opacity(0.35))
            Text(title)
                .font(.appHeadlineSmall)
                .foregroundStyle(AppTheme.Palette.textPrimary)
            Text("Lands in Phase iOS-\(phaseNumber).")
                .font(.appBodySmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background(AppTheme.Palette.appBackground)
    }
}
