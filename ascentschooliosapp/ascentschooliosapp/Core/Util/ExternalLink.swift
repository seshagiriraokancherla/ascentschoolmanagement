import SwiftUI
import UIKit

enum ExternalLink {

    @MainActor
    static func open(_ urlString: String?) {
        guard let str = urlString?.trimmingCharacters(in: .whitespacesAndNewlines),
              !str.isEmpty,
              let url = URL(string: str) else { return }
        UIApplication.shared.open(url)
    }

    @MainActor
    static func open(_ url: URL?) {
        guard let url else { return }
        UIApplication.shared.open(url)
    }
}

// Capsule button that opens a URL via `UIApplication.shared.open`.
// Used by Homework / Announcements / Events for attachment links.
struct ExternalLinkButton: View {
    let title: String
    let systemImage: String
    let urlString: String?

    var body: some View {
        Button {
            ExternalLink.open(urlString)
        } label: {
            HStack(spacing: 6) {
                Image(systemName: systemImage)
                Text(title)
            }
            .font(.appLabelMedium.bold())
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .foregroundStyle(AppTheme.Palette.onNavyContainer)
            .background(AppTheme.Palette.navyContainer, in: Capsule())
        }
        .buttonStyle(.plain)
    }
}
