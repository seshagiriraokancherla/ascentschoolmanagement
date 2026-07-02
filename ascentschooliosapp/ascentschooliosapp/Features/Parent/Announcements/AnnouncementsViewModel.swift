import Foundation
import Observation

@Observable
final class AnnouncementsViewModel {

    enum State {
        case idle
        case loading
        case success([AnnouncementDto])
        case failure(String)
    }

    var state: State = .idle

    func load() async {
        state = .loading
        do {
            let raw = try await APIClient.shared.studentAnnouncements()
            // Pinned-first sort to mirror Android `AnnouncementsScreen.kt`
            let sorted = raw.sorted { lhs, rhs in
                if (lhs.isPinned ?? false) != (rhs.isPinned ?? false) {
                    return (lhs.isPinned ?? false)
                }
                return (lhs.publishedDate ?? "") > (rhs.publishedDate ?? "")
            }
            state = .success(sorted)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }
}
