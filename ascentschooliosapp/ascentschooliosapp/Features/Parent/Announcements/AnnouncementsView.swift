import SwiftUI

struct AnnouncementsView: View {
    @State private var viewModel = AnnouncementsViewModel()

    var body: some View {
        ScrollView {
            content
                .padding(.horizontal, 16)
                .padding(.vertical, 14)
                .frame(maxWidth: .infinity)
        }
        .background(AppTheme.Palette.appBackground)
        .refreshable {
            await viewModel.load()
        }
        .task {
            if case .idle = viewModel.state {
                await viewModel.load()
            }
        }
    }

    @ViewBuilder
    private var content: some View {
        switch viewModel.state {
        case .idle, .loading:
            VStack(spacing: 12) {
                LoadingCard()
                LoadingCard()
                LoadingCard()
            }
        case .success(let items):
            if items.isEmpty {
                EmptyState(
                    systemImage: "megaphone",
                    title: "No notices",
                    message: "Nothing new from the school right now."
                )
                .frame(minHeight: 320)
            } else {
                VStack(spacing: 12) {
                    ForEach(items) { item in
                        card(item)
                    }
                }
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(minHeight: 280)
        }
    }

    private func card(_ item: AnnouncementDto) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(spacing: 6) {
                if item.isPinned == true {
                    HStack(spacing: 3) {
                        Image(systemName: "pin.fill")
                        Text("Pinned")
                    }
                    .font(.appLabelSmall.bold())
                    .foregroundStyle(.white)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 3)
                    .background(AppTheme.Palette.gold, in: Capsule())
                }

                if let scope = item.scope, !scope.isEmpty {
                    Text(scope.uppercased())
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(AppTheme.Palette.navyContainer, in: Capsule())
                }

                Spacer()

                if let date = item.publishedDate, !date.isEmpty {
                    Text(date.friendlyDate())
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
            }

            if let title = item.title, !title.isEmpty {
                Text(title)
                    .font(.appTitleMedium)
                    .foregroundStyle(AppTheme.Palette.textPrimary)
            }

            if let desc = item.description, !desc.isEmpty {
                Text(desc)
                    .font(.appBodySmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                    .fixedSize(horizontal: false, vertical: true)
            }

            if let url = item.attachmentUrl, !url.isEmpty {
                ExternalLinkButton(
                    title: "Open attachment",
                    systemImage: "doc.text",
                    urlString: url
                )
                .padding(.top, 4)
            }
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
        .overlay(
            RoundedRectangle(cornerRadius: 16)
                .stroke(
                    item.isPinned == true ? AppTheme.Palette.gold.opacity(0.4) : Color.clear,
                    lineWidth: 1.2
                )
        )
    }
}
