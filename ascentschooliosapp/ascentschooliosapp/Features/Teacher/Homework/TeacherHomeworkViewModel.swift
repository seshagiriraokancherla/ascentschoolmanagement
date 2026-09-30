import Foundation
import Observation

@Observable
final class TeacherHomeworkViewModel {

    enum HistoryState {
        case idle
        case loading
        case success([TeacherHomeworkDto])
        case failure(String)
    }

    enum Tab: Hashable { case history, create }

    let classId: Int

    var selectedTab: Tab = .history
    var historyState: HistoryState = .idle

    // Create form (Phase 93: due date retired — no dueDate field)
    var title: String = ""
    var description: String = ""
    var assignedDate: Date = Date()

    var isCreating: Bool = false
    var createError: String?
    var createSuccess: Bool = false

    init(classId: Int) {
        self.classId = classId
    }

    func loadHistory() async {
        historyState = .loading
        do {
            let items = try await APIClient.shared.teacherHomework(classId: classId)
            historyState = .success(items.sorted { ($0.assignedDate ?? "") > ($1.assignedDate ?? "") })
        } catch {
            historyState = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }

    func create() async {
        let trimmedTitle = title.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedTitle.isEmpty else {
            createError = "Enter a title."
            return
        }

        isCreating = true
        createError = nil
        defer { isCreating = false }

        let df = DateFormatter()
        df.locale = Locale(identifier: "en_US_POSIX")
        df.dateFormat = "yyyy-MM-dd"

        let request = TeacherCreateHomeworkRequest(
            classId: classId,
            sectionId: nil,
            subjectId: nil,
            title: trimmedTitle,
            description: description.isEmpty ? nil : description,
            assignedDate: df.string(from: assignedDate)
        )

        do {
            _ = try await APIClient.shared.createTeacherHomework(request)
            createSuccess = true
            clearForm()
            await loadHistory()
            selectedTab = .history
        } catch {
            createError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func clearForm() {
        title = ""
        description = ""
        assignedDate = Date()
    }

    func dismissCreateSuccess() { createSuccess = false }
    func dismissCreateError()   { createError = nil }
}
