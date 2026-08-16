import SwiftUI

// Phase 92 (Android parity): parent chat with the child's assigned class
// teacher(s). One continuous thread scoped to the selected child.
struct MessagesView: View {
    @State private var viewModel = MessagesViewModel()

    // Report flow (UGC).
    @State private var reportTargetId: Int?
    @State private var reportReason: String = ""

    var body: some View {
        VStack(spacing: 0) {
            recipientHeader
            content
            composer
        }
        .background(AppTheme.Palette.appBackground)
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

    // MARK: - Header

    @ViewBuilder
    private var recipientHeader: some View {
        HStack(spacing: 8) {
            Image(systemName: "person.2.fill")
                .foregroundStyle(AppTheme.Palette.onNavyContainer)
            VStack(alignment: .leading, spacing: 1) {
                Text("Class teacher\(viewModel.teacherNames.count > 1 ? "s" : "")")
                    .font(.appLabelSmall)
                    .foregroundStyle(AppTheme.Palette.textSecondary)
                Text(viewModel.teacherNames.isEmpty ? "—" : viewModel.teacherNames.joined(separator: ", "))
                    .font(.appLabelLarge.bold())
                    .foregroundStyle(AppTheme.Palette.textPrimary)
                    .lineLimit(1)
            }
            Spacer()
            if viewModel.hasThread {
                blockMenu
            }
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
        .background(AppTheme.Palette.navyContainer)
    }

    private var blockMenu: some View {
        Menu {
            if viewModel.isBlocked {
                if viewModel.blockedByParent {
                    Button {
                        Task { await viewModel.unblock() }
                    } label: {
                        Label("Unblock", systemImage: "lock.open")
                    }
                } else {
                    // Blocked by the school — the parent can't lift it.
                    Text("Blocked by the school")
                }
            } else {
                Button(role: .destructive) {
                    Task { await viewModel.block() }
                } label: {
                    Label("Block conversation", systemImage: "hand.raised")
                }
            }
        } label: {
            Image(systemName: "ellipsis.circle")
                .imageScale(.large)
                .foregroundStyle(AppTheme.Palette.onNavyContainer)
        }
        .accessibilityLabel("Conversation options")
    }

    // MARK: - Content

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
                    title: viewModel.canMessage ? "No messages yet" : "Messaging unavailable",
                    message: viewModel.canMessage
                        ? "Send the first message to your child's class teacher."
                        : (viewModel.reason ?? "Messaging isn't available right now.")
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
                            isMine: message.senderType.caseInsensitiveCompare("parent") == .orderedSame,
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

    // MARK: - Composer

    @ViewBuilder
    private var composer: some View {
        if viewModel.canMessage {
            HStack(spacing: 10) {
                TextField("Message…", text: $viewModel.composer, axis: .vertical)
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
        } else if case .success = viewModel.state {
            // Blocked / no teacher assigned — explain why the composer is gone.
            Text(viewModel.reason ?? "You can't send messages right now.")
                .font(.appLabelSmall)
                .foregroundStyle(AppTheme.Palette.textSecondary)
                .multilineTextAlignment(.center)
                .frame(maxWidth: .infinity)
                .padding(.horizontal, 16)
                .padding(.vertical, 12)
                .background(AppTheme.Palette.surfaceVariant.opacity(0.4))
        }
    }

    // MARK: - Bindings

    private var reportBinding: Binding<Bool> {
        Binding(get: { reportTargetId != nil }, set: { if !$0 { reportTargetId = nil } })
    }

    private var actionErrorBinding: Binding<Bool> {
        Binding(get: { viewModel.actionError != nil }, set: { if !$0 { viewModel.dismissActionError() } })
    }
}
