import Foundation
import Observation

// Phase 92 (Android parity): parent side of parent↔teacher messaging. Scoped to
// the selected child (one thread, no list). A single GET returns whether
// messaging is available, who it reaches, and the conversation.
@Observable
final class MessagesViewModel {

    enum State {
        case idle
        case loading
        case success(ParentThreadViewDto)
        case failure(String)
    }

    var state: State = .idle
    var composer: String = ""
    var isSending: Bool = false
    var actionError: String?

    // MARK: - Derived (nil-safe accessors over the loaded view)

    private var view: ParentThreadViewDto? {
        if case .success(let v) = state { return v }
        return nil
    }

    var messages: [MessageDto] { view?.messages ?? [] }
    var canMessage: Bool { view?.canMessage ?? false }
    var reason: String? { view?.reason }
    var teacherNames: [String] { view?.teachers ?? [] }
    var status: String { view?.status ?? "Active" }
    var isBlocked: Bool { status.caseInsensitiveCompare("Blocked") == .orderedSame }
    var blockedByParent: Bool { (view?.blockedByType ?? "") == "parent" }
    var hasThread: Bool { view?.threadId != nil }

    // MARK: - Load

    func load(markRead: Bool = true) async {
        if case .success = state {} else { state = .loading }
        do {
            let v = try await APIClient.shared.parentThread()
            state = .success(v)
            if markRead, v.threadId != nil {
                // Fire-and-forget — read receipts are best-effort.
                try? await APIClient.shared.markParentThreadRead()
            }
        } catch {
            state = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }

    // MARK: - Actions

    func send() async {
        let body = composer.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !body.isEmpty else { return }
        isSending = true
        defer { isSending = false }
        do {
            _ = try await APIClient.shared.sendParentMessage(body: body)
            composer = ""
            await load(markRead: false)   // refresh thread with the new message
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func report(messageId: Int, reason: String?) async {
        do {
            try await APIClient.shared.reportParentMessage(messageId: messageId, reason: reason)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func block() async {
        do {
            try await APIClient.shared.blockParentThread()
            await load(markRead: false)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func unblock() async {
        do {
            try await APIClient.shared.unblockParentThread()
            await load(markRead: false)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func dismissActionError() { actionError = nil }
}
