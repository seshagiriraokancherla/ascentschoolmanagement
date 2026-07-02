import SwiftUI

// Skeleton row used while data is loading. Pair with `.shimmer()` for movement.
struct LoadingCard: View {
    var lineCount: Int = 3
    var height: CGFloat = 88

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            ForEach(0..<lineCount, id: \.self) { index in
                RoundedRectangle(cornerRadius: 6)
                    .fill(AppTheme.Palette.surfaceVariant)
                    .frame(height: index == 0 ? 14 : 10)
                    .frame(maxWidth: index == lineCount - 1 ? 160 : .infinity, alignment: .leading)
            }
        }
        .padding(16)
        .frame(height: height, alignment: .topLeading)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
        .shimmer()
        // Skeletons are decorative; hide them from VoiceOver so screen readers
        // don't announce "Loading. Loading. Loading." per skeleton card. The
        // screen's real content will be announced once it loads.
        .accessibilityHidden(true)
    }
}

#Preview {
    VStack(spacing: 12) {
        LoadingCard()
        LoadingCard(lineCount: 2, height: 64)
    }
    .padding()
    .background(AppTheme.Palette.appBackground)
}
