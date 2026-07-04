import Foundation
import Observation

@Observable
final class TeacherAttendanceViewModel {

    let classId: Int
    let sectionId: Int

    var date: Date = Date()

    var students: [TeacherAttendanceStudentDto] = []
    var isMarked: Bool = false

    var isLoading: Bool = false
    var loadError: String?

    var isSaving: Bool = false
    var saveError: String?
    var saveSuccess: Bool = false

    init(classId: Int, sectionId: Int) {
        self.classId = classId
        self.sectionId = sectionId
    }

    // MARK: - Derived counts

    var presentCount: Int { students.filter { $0.status?.caseInsensitiveCompare("Present") == .orderedSame }.count }
    var absentCount:  Int { students.filter { $0.status?.caseInsensitiveCompare("Absent")  == .orderedSame }.count }
    var lateCount:    Int { students.filter { $0.status?.caseInsensitiveCompare("Late")    == .orderedSame }.count }
    var unmarkedCount: Int { students.filter { ($0.status ?? "").isEmpty }.count }

    var hasAnyStatusSet: Bool { students.contains { ($0.status ?? "").isEmpty == false } }

    var dateString: String {
        let df = DateFormatter()
        df.locale = Locale(identifier: "en_US_POSIX")
        df.dateFormat = "yyyy-MM-dd"
        return df.string(from: date)
    }

    // MARK: - Load

    func load() async {
        isLoading = true
        loadError = nil
        defer { isLoading = false }
        do {
            let grid = try await APIClient.shared.teacherAttendance(
                classId: classId,
                sectionId: sectionId,
                date: dateString
            )
            students = grid.students
            isMarked = grid.isMarked
        } catch {
            loadError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    // MARK: - Mutations

    // Tap-to-cycle: empty → Present → Absent → Late → Present
    func cycleStatus(at index: Int) {
        guard students.indices.contains(index) else { return }
        let next: String
        switch students[index].status?.capitalized {
        case "Present": next = "Absent"
        case "Absent":  next = "Late"
        case "Late":    next = "Present"
        default:        next = "Present"
        }
        students[index].status = next
    }

    func setStatus(at index: Int, status: String) {
        guard students.indices.contains(index) else { return }
        students[index].status = status
    }

    func markAllPresent() {
        for i in students.indices {
            students[i].status = "Present"
        }
    }

    // MARK: - Save

    func save() async {
        saveError = nil
        saveSuccess = false

        let entries: [TeacherAttendanceEntry] = students.compactMap { s in
            guard let status = s.status, !status.isEmpty else { return nil }
            return TeacherAttendanceEntry(
                studentId: s.studentId,
                status: status,
                remarks: s.remarks
            )
        }

        guard !entries.isEmpty else {
            saveError = "Mark at least one student before saving."
            return
        }

        isSaving = true
        defer { isSaving = false }

        let request = TeacherSaveAttendanceRequest(
            classId: classId,
            sectionId: sectionId,
            date: dateString,
            entries: entries
        )

        do {
            try await APIClient.shared.saveTeacherAttendance(request)
            saveSuccess = true
            // Reload so `isMarked` flips to true and we see canonical server state.
            await load()
        } catch {
            saveError = (error as? APIError)?.errorDescription ?? error.localizedDescription
        }
    }

    func dismissSaveSuccess() { saveSuccess = false }
    func dismissSaveError()   { saveError = nil }
}
