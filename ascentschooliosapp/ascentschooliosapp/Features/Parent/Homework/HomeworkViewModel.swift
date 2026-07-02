import Foundation
import Observation

@Observable
final class HomeworkViewModel {

    enum State {
        case idle
        case loading
        case success([HomeworkDto])
        case failure(String)
    }

    var state: State = .idle

    func load() async {
        state = .loading
        do {
            let items = try await APIClient.shared.studentHomework()
            state = .success(items)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }
}
