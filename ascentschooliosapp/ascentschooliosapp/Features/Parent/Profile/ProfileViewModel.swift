import Foundation
import Observation

@Observable
final class ProfileViewModel {

    enum State {
        case idle
        case loading
        case success(StudentProfileDto)
        case failure(String)
    }

    var state: State = .idle

    func load() async {
        state = .loading
        do {
            let profile = try await APIClient.shared.studentProfile()
            state = .success(profile)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }
}
