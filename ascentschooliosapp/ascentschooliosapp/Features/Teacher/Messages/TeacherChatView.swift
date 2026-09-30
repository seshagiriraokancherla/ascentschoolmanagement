import SwiftUI

// Phase 92 (Android parity): teacher's chat with one parent about one child.
struct TeacherChatView: View {
    @State private var viewModel: TeacherChatViewModel
    private let title: String

    @State private var reportTargetId: Int?
    @State private var reportReason: String = ""

    init(threadId: Int, title: String) {
        _viewModel = State(initialValue: TeacherChatViewModel(threadId: threadId))
        self.title = title
    }

    var body: some View {
        VStack(spacing: 0) {
            content
            composer
        }
        .background(AppTheme.Palette.appBackground)
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
        .toolbarBackground(AppTheme.Palette.navyBlue, for: .navigationBar)
        .toolbarBackground(.visible, for: .navigationBar)
        .toolbarColorScheme(.dark, for: .navigationBar)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                blockMenu
            }
        }
        .task {
            if case .idle = viewModel.state {
                await viewModel.load()
            }
        }
        .alert("Report message", isPresented: reportBinding) {
            TextField("Reason (optional)", text: $reportReason)
            Button("Report", role: .destructive) {
                if let id = reportTargetId {
                    let reason = reportReason
                    Task { await viewModel.report(messageId: id, reason: reason.isEmpty ? nil : reason) }
                }
                reportTargetId = nil
                reportReason = ""
            }
            Button("Cancel", role: .cancel) {
                reportTargetId = nil
                reportReason = ""
            }
        } message: {
            Text("The school will review this message.")
        }
        .alert("Something went wrong", isPresented: actionErrorBinding) {
            Button("OK", role: .cancel) { viewModel.dismissActionError() }
        } message: {
            Text(viewModel.actionError ?? "")
        }
    }

    private var blockMenu: some View {
        Menu {
            if viewModel.isBlocked {
                if viewModel.blockedByParent {
                    Text("Blocked by the parent")
                } else {
                    Button {
                        Task { await viewModel.unblock() }
                    } label: {
                        Label("Unblock", systemImage: "lock.open")
                    }
                }
            } else {
                Button(role: .destructive) {
                    Task { await viewModel.block() }
                } label: {
                    Label("Block conversation", systemImage: "hand.raised")
                }
            }
        } label: {
            Image(systemName: "ellipsis.circle").imageScale(.large)
        }
        .tint(.white)
        .accessibilityLabel("Conversation options")
    }

    @ViewBuilder
    private var content: some View {
        switch viewModel.state {
        case .idle, .loading:
            VStack(spacing: 12) { LoadingCard(); LoadingCard() }
                .padding(16)
                .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
        case .success:
            if viewModel.messages.isEmpty {
                EmptyState(
                    systemImage: "bubble.left.and.bubble.right",
                    title: "No messages yet",
                    message: "Reply below to start the conversation."
                )
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                messageList
            }
        case .failure(let message):
            ErrorView(message: message) {
                Task { await viewModel.load() }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }

    private var messageList: some View {
        ScrollViewReader { proxy in
            ScrollView {
                LazyVStack(spacing: 10) {
                    ForEach(viewModel.messages) { message in
                        MessageBubble(
                            message: message,
                            isMine: message.senderType.caseInsensitiveCompare("teacher") == .orderedSame,
                            onReport: { id in
                                reportTargetId = id
                                reportReason = ""
                            }
                        )
                        .id(message.messageId)
                    }
                }
                .padding(.horizontal, 14)
                .padding(.vertical, 12)
            }
            .refreshable { await viewModel.load() }
            .onChange(of: viewModel.messages.count) { _, _ in
                if let last = viewModel.messages.last {
                    withAnimation { proxy.scrollTo(last.messageId, anchor: .bottom) }
                }
            }
            .onAppear {
                if let last = viewModel.messages.last {
                    proxy.scrollTo(last.messageId, anchor: .bottom)
                }
            }
        }
    }

    @ViewBuilder
    private var composer: some View {
        if viewModel.isBlocked {
            Text(viewModel.blockedByParent
                 ? "This conversation was blocked by the parent."
                 : "This conversation is blocked.")
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .multilineTextAlignment(.center)
                .frame(maxWidth: .infinity)
                .padding(.horizontal, 16)
                .padding(.vertical, 12)
                .background(AppTheme.Palette.surfaceVariant.opacity(0.4))
        } else {
            HStack(spacing: 10) {
                TextField("Reply…", text: $viewModel.composer, axis: .vertical)
                    .lineLimit(1...4)
                    .padding(.vertical, 8)
                    .padding(.horizontal, 12)
                    .background(AppTheme.Palette.appSurface, in: RoundedRectangle(cornerRadius: 18))
                    .overlay(
                        RoundedRectangle(cornerRadius: 18)
                            .stroke(AppTheme.Palette.surfaceVariant, lineWidth: 1)
                    )

                Button {
                    Task { await viewModel.send() }
                } label: {
                    Image(systemName: viewModel.isSending ? "hourglass" : "paperplane.fill")
                        .imageScale(.large)
                        .foregroundStyle(.white)
                        .frame(width: 40, height: 40)
                        .background(AppTheme.Palette.navyBlue, in: Circle())
                }
                .disabled(viewModel.isSending || viewModel.composer.trimmingCharacters(in: .whitespaces).isEmpty)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(AppTheme.Palette.appBackground)
        }
    }

    private var reportBinding: Binding<Bool> {
        Binding(get: { reportTargetId != nil }, set: { if !$0 { reportTargetId = nil } })
    }

    private var actionErrorBinding: Binding<Bool> {
        Binding(get: { viewModel.actionError != nil }, set: { if !$0 { viewModel.dismissActionError() } })
    }
}
