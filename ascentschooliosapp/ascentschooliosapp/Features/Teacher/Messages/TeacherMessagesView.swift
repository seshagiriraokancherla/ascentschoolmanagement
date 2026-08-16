import SwiftUI

// Phase 92 (Android parity): teacher inbox — threads across all assigned
// classes. Tapping a row opens the chat.
struct TeacherMessagesView: View {
    @State private var viewModel = TeacherMessagesViewModel()

    var body: some View {
        content
            .background(AppTheme.Palette.appBackground)
            .navigationTitle("Messages")
            .navigationBarTitleDisplayMode(.inline)
            .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
            .toolbarBackground(.visible, for: .navigationBar)
            .toolbarColorScheme(.dark, for: .navigationBar)
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
            ScrollView {
                VStack(spacing: 12) { LoadingCard(); LoadingCard(); LoadingCard() }
                    .padding(16)
            }
        case .success(let threads):
            if threads.isEmpty {
                EmptyState(
                    systemImage: "bubble.left.and.bubble.right",
                    title: "No conversations",
                    message: "When a parent messages you, it'll show up here."
                )
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                ScrollView {
                    LazyVStack(spacing: 10) {
                        ForEach(threads) { thread in
                            NavigationLink {
                                TeacherChatView(threadId: thread.threadId, title: thread.studentName ?? "Conversation")
                            } label: {
                                row(thread)
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 14)
                }
                .refreshable { await viewModel.load() }
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }

    private func row(_ thread: MessageThreadDto) -> some View {
        HStack(spacing: 12) {
            Circle()
                .fill(AppTheme.Palette.navyContainer)
                .frame(width: 44, height: 44)
                .overlay(
                    Text(initials(for: thread.studentName ?? "?"))
                        .font(.appLabelLarge)
                        .foregroundStyle(AppTheme.Palette.onNavyContainer)
                )

            VStack(alignment: .leading, spacing: 3) {
                HStack {
                    Text(thread.studentName ?? "Student")
                        .font(.appTitleMedium)
                        .foregroundStyle(AppTheme.Palette.textPrimary)
                        .lineLimit(1)
                    Spacer()
                    if let at = thread.lastMessageAt {
                        Text(at.friendlyDate(style: "d MMM"))
                            .font(.appLabelSmall)
                            .foregroundStyle(AppTheme.Palette.textSecondary)
                    }
                }

                if let cls = classLabel(thread) {
                    Text(cls)
                        .font(.appLabelSmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                }

                HStack(spacing: 8) {
                    Text(thread.lastMessageBody ?? "No messages yet")
                        .font(.appBodySmall)
                        .foregroundStyle(AppTheme.Palette.textSecondary)
                        .lineLimit(1)
                    Spacer()
                    if let unread = thread.unreadCount, unread > 0 {
                        Text("\(unread)")
                            .font(.appLabelSmall.bold())
                            .foregroundStyle(.white)
                            .padding(.horizontal, 7)
                            .padding(.vertical, 2)
                            .background(AppTheme.Palette.absent, in: Capsule())
                    }
                }
            }
        }
        .padding(12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 14))
    }

    private func classLabel(_ thread: MessageThreadDto) -> String? {
        let cls = (thread.className ?? "").trimmingCharacters(in: .whitespaces)
        let sec = (thread.sectionName ?? "").trimmingCharacters(in: .whitespaces)
        if cls.isEmpty && sec.isEmpty { return nil }
        if sec.isEmpty { return cls }
        return "\(cls) · \(sec)"
    }

    private func initials(for name: String) -> String {
        let parts = name.split(separator: " ").prefix(2).compactMap { $0.first.map(String.init) }
        return parts.joined().uppercased()
    }
}
