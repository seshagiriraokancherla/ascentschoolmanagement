import SwiftUI

struct EventsView: View {
    @State private var viewModel = EventsViewModel()

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
                LoadingCard(lineCount: 1, height: 220)
                LoadingCard(lineCount: 1, height: 220)
            }
        case .success(let items):
            if items.isEmpty {
                EmptyState(
                    systemImage: "photo.on.rectangle.angled",
                    title: "No events yet",
                    message: "Photos and videos from school events will show up here."
                )
                .frame(minHeight: 320)
            } else {
                VStack(spacing: 14) {
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

    // MARK: - Card

    private func card(_ event: SchoolEventDto) -> some View {
        let isVideo = (event.mediaType?.lowercased() == "video")

        return Button {
            // Tapping the card opens the media URL (YouTube / image / etc.).
            ExternalLink.open(event.mediaUrl)
        } label: {
            VStack(alignment: .leading, spacing: 0) {
                thumbnail(event, isVideo: isVideo)

                VStack(alignment: .leading, spacing: 8) {
                    HStack(spacing: 6) {
                        if event.isPinned == true {
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
                        Spacer()
                        if let date = event.eventDate, !date.isEmpty {
                            Text(date.friendlyDate())
                                .font(.appLabelSmall)
                                .foregroundStyle(AppTheme.Palette.textSecondary)
                        }
                    }

                    if let title = event.title, !title.isEmpty {
                        Text(title)
                            .font(.appTitleMedium)
                            .foregroundStyle(AppTheme.Palette.textPrimary)
                            .multilineTextAlignment(.leading)
                    }

                    if let desc = event.description, !desc.isEmpty {
                        Text(desc)
                            .font(.appBodySmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                            .multilineTextAlignment(.leading)
                            .fixedSize(horizontal: false, vertical: true)
                    }

                    if let attachmentUrl = event.attachmentUrl, !attachmentUrl.isEmpty {
                        ExternalLinkButton(
                            title: "Open attachment",
                            systemImage: "doc.text",
                            urlString: attachmentUrl
                        )
                        .padding(.top, 2)
                    }
                }
                .padding(14)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
            .clipShape(RoundedRectangle(cornerRadius: 16))
        }
        .buttonStyle(.plain)
    }

    @ViewBuilder
    private func thumbnail(_ event: SchoolEventDto, isVideo: Bool) -> some View {
        let imageURLString = event.thumbnailUrl ?? event.mediaUrl
        let url = URL(string: imageURLString ?? "")

        ZStack {
            if let url {
                AsyncImage(url: url) { phase in
                    switch phase {
                    case .empty:
                        placeholder
                    case .success(let image):
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                    case .failure:
                        placeholder
                    @unknown default:
                        placeholder
                    }
                }
            } else {
                placeholder
            }

            if isVideo {
                Color.black.opacity(0.35)
                VStack(spacing: 4) {
                    Image(systemName: "play.circle.fill")
                        .font(.system(size: 56))
                        .foregroundStyle(.white)
                        .shadow(color: .black.opacity(0.4), radius: 6, y: 2)
                    Text("WATCH")
                        .font(.appLabelSmall.bold())
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(Color.red.opacity(0.9), in: Capsule())
                }
            }
        }
        .frame(height: 200)
        .frame(maxWidth: .infinity)
        .clipped()
    }

    private var placeholder: some View {
        ZStack {
            AppTheme.Palette.surfaceVariant
            Image(systemName: "photo")
                .font(.system(size: 36))
                .foregroundStyle(AppTheme.Palette.textSecondary.opacity(0.5))
        }
    }
}
