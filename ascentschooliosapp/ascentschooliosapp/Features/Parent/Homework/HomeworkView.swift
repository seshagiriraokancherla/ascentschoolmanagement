import SwiftUI

struct HomeworkView: View {
    @State private var viewModel = HomeworkViewModel()

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
                    systemImage: "tray",
                    title: "No homework",
                    message: "Nothing assigned yet. Check back after your teacher posts the next task."
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

    private func card(_ item: HomeworkDto) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            // Phase 93 (Android parity): "due date" retired everywhere — schools
            // didn't use it. Only the subject label remains on this row.
            if let subject = item.subjectName, !subject.isEmpty {
                Text(subject.uppercased())
                    .font(.appLabelSmall.bold())
                    .foregroundStyle(AppTheme.Palette.navyBlue)
                    .frame(maxWidth: .infinity, alignment: .leading)
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

            HStack {
                if let assigned = item.assignedDate, !assigned.isEmpty {
                    Label(assigned.friendlyDate(), systemImage: "calendar")
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }
                Spacer()
            }

            // Per-file attachments (new-format `attachments[]`)
            if let attachments = item.attachments, !attachments.isEmpty {
                VStack(alignment: .leading, spacing: 6) {
                    ForEach(attachments) { attachment in
                        ExternalLinkButton(
                            title: attachment.fileName ?? "Attachment",
                            systemImage: "paperclip",
                            urlString: attachment.fileUrl
                        )
                    }
                }
                .padding(.top, 4)
            }

            // Single attachment URL (older homework rows)
            if let url = item.attachmentUrl, !url.isEmpty {
                ExternalLinkButton(
                    title: "View attachment",
                    systemImage: "doc.text",
                    urlString: url
                )
                .padding(.top, 4)
            }
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 16))
    }
}
