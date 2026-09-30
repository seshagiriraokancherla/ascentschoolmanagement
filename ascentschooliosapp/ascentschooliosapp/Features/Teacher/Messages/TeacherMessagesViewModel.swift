import Foundation
import Observation

// Phase 92 (Android parity): teacher inbox — the threads of children in the
// classes this teacher is assigned to. Spans all assigned classes (not gated
// by the home-screen class picker).
@Observable
final class TeacherMessagesViewModel {

    enum State {
        case idle
        case loading
        case success([MessageThreadDto])
        case failure(String)
    }

    var state: State = .idle

    func load() async {
        if case .success = state {} else { state = .loading }
        do {
            let threads = try await APIClient.shared.teacherThreads()
            // Most-recent activity first; unread float naturally via lastMessageAt.
            state = .success(threads.sorted { ($0.lastMessageAt ?? "") > ($1.lastMessageAt ?? "") })
        } catch {
            state = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }
}
