import Foundation
import Observation

// Phase 92 (Android parity): one teacher↔parent thread. Any assigned teacher
// can reply; the thread is shared across them.
@Observable
final class TeacherChatViewModel {

    enum State {
        case idle
        case loading
        case success(MessageThreadDetailDto)
        case failure(String)
    }

    let threadId: Int

    var state: State = .idle
    var composer: String = ""
    var isSending: Bool = false
    var actionError: String?

    init(threadId: Int) {
        self.threadId = threadId
    }

    private var detail: MessageThreadDetailDto? {
        if case .success(let d) = state { return d }
        return nil
    }

    var messages: [MessageDto] { detail?.messages ?? [] }
    var thread: MessageThreadDto? { detail?.thread }
    var status: String { detail?.thread?.status ?? "Active" }
    var isBlocked: Bool { status.caseInsensitiveCompare("Blocked") == .orderedSame }
    var blockedByParent: Bool { (detail?.thread?.blockedByType ?? "") == "parent" }

    func load(markRead: Bool = true) async {
        if case .success = state {} else { state = .loading }
        do {
            let d = try await APIClient.shared.teacherThread(threadId: threadId)
            state = .success(d)
            if markRead {
                try? await APIClient.shared.markTeacherThreadRead(threadId: threadId)
            }
        } catch {
            state = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }

    func send() async {
        let body = composer.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !body.isEmpty else { return }
        isSending = true
        defer { isSending = false }
        do {
            _ = try await APIClient.shared.replyTeacherMessage(threadId: threadId, body: body)
            composer = ""
            await load(markRead: false)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func report(messageId: Int, reason: String?) async {
        do {
            try await APIClient.shared.reportTeacherMessage(threadId: threadId, messageId: messageId, reason: reason)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func block() async {
        do {
            try await APIClient.shared.blockTeacherThread(threadId: threadId)
            await load(markRead: false)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func unblock() async {
        do {
            try await APIClient.shared.unblockTeacherThread(threadId: threadId)
            await load(markRead: false)
        } catch {
            actionError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func dismissActionError() { actionError = nil }
}
