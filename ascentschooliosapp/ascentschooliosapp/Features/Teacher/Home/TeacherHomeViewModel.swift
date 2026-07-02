import Foundation
import Observation

@Observable
final class TeacherHomeViewModel {

    // Classes
    var classes: [TeacherClassDto] = []
    var isLoadingClasses: Bool = false
    var classesError: String?

    // Sections (cascaded by selected class)
    var sections: [TeacherSectionDto] = []
    var isLoadingSections: Bool = false
    var sectionsError: String?

    // Selection
    var selectedClassId: Int?
    var selectedSectionId: Int?

    var isReadyForAction: Bool {
        selectedClassId != nil && selectedSectionId != nil
    }

    var selectedClassName: String? {
        guard let id = selectedClassId else { return nil }
        return classes.first { $0.classId == id }?.className
    }

    var selectedSectionName: String? {
        guard let id = selectedSectionId else { return nil }
        return sections.first { $0.sectionId == id }?.sectionName
    }

    func loadClasses() async {
        isLoadingClasses = true
        classesError = nil
        defer { isLoadingClasses = false }
        do {
            classes = try await APIClient.shared.teacherClasses()
        } catch {
            classesError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func loadSections() async {
        sections = []
        selectedSectionId = nil
        guard let classId = selectedClassId else { return }

        isLoadingSections = true
        sectionsError = nil
        defer { isLoadingSections = false }
        do {
            sections = try await APIClient.shared.teacherSections(classId: classId)
        } catch {
            sectionsError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }
}
