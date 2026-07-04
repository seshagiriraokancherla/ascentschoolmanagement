import Foundation
import Observation

@Observable
final class MarksViewModel {

    enum State {
        case idle
        case loading
        case success([MarksResultDto])
        case failure(String)
    }

    var state: State = .idle

    // nil → server picks "current" academic year.
    var selectedAcademicYearId: Int?

    func load() async {
        state = .loading
        do {
            let results = try await APIClient.shared.studentMarks(academicYearId: selectedAcademicYearId)
            state = .success(results)
        } catch {
            let msg = (error as? APIError)?.errorDescription ?? error.localizedDescription
            state = .failure(msg)
        }
    }
}
