import Foundation
import Observation

// Phase 90 (Android parity): teachers post announcements to their class,
// optionally targeting a single section. Mirrors TeacherHomeworkViewModel's
// History | Create shape. Sections are loaded for the optional target picker;
// leaving it on "All sections" posts class-wide.
@Observable
final class TeacherAnnouncementViewModel {

    enum HistoryState {
        case idle
        case loading
        case success([TeacherAnnouncementDto])
        case failure(String)
    }

    enum Tab: Hashable { case history, create }

    let classId: Int

    var selectedTab: Tab = .history
    var historyState: HistoryState = .idle

    // Sections for the optional target picker (nil selection = whole class).
    var sections: [TeacherSectionDto] = []
    var selectedSectionId: Int?

    // Create form
    var title: String = ""
    var body: String = ""

    var isCreating: Bool = false
    var createError: String?
    var createSuccess: Bool = false

    init(classId: Int) {
        self.classId = classId
    }

    var selectedSectionName: String {
        guard let id = selectedSectionId,
              let match = sections.first(where: { $0.sectionId == id }) else {
            return "All sections"
        }
        return match.sectionName
    }

    func loadHistory() async {
        historyState = .loading
        do {
            let items = try await APIClient.shared.teacherAnnouncements(classId: classId)
            // Pinned first, then most recent.
            historyState = .success(items.sorted { lhs, rhs in
                if (lhs.isPinned ?? false) != (rhs.isPinned ?? false) {
                    return (lhs.isPinned ?? false)
                }
                return (lhs.publishedDate ?? "") > (rhs.publishedDate ?? "")
            })
        } catch {
            historyState = .failure((error as? APIError)?.errorDescription ?? error.localizedDescription)
        }
    }

    // Best-effort — the picker just falls back to "All sections" if this fails.
    func loadSections() async {
        do {
            sections = try await APIClient.shared.teacherSections(classId: classId)
        } catch {
            sections = []
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

        let request = TeacherCreateAnnouncementRequest(
            classId: classId,
            sectionId: selectedSectionId,
            title: trimmedTitle,
            description: body.isEmpty ? nil : body
        )

        do {
            _ = try await APIClient.shared.createTeacherAnnouncement(request)
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
        body = ""
        selectedSectionId = nil
    }

    func dismissCreateSuccess() { createSuccess = false }
    func dismissCreateError()   { createError = nil }
}
