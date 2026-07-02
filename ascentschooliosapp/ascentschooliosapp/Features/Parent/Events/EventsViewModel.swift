import Foundation
import Observation

@Observable
final class EventsViewModel {

    enum State {
        case idle
        case loading
        case success([SchoolEventDto])
        case failure(String)
    }

    var state: State = .idle

    func load() async {
        state = .loading
        do {
            let raw = try await APIClient.shared.studentEvents()
            // Pinned first, then most-recent eventDate.
            let sorted = raw.sorted { lhs, rhs in
                if (lhs.isPinned ?? false) != (rhs.isPinned ?? false) {
                    return (lhs.isPinned ?? false)
                }
                return (lhs.eventDate ?? "") > (rhs.eventDate ?? "")
            }
            state = .success(sorted)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }
}
