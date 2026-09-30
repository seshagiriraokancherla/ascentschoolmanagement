import Foundation

// MARK: - Lookups

struct TeacherClassDto: Decodable, Identifiable {
    let classId: Int
    let className: String

    var id: Int { classId }
}

struct TeacherSectionDto: Decodable, Identifiable {
    let sectionId: Int
    let sectionName: String

    var id: Int { sectionId }
}

// MARK: - Attendance

struct TeacherAttendanceGridDto: Decodable {
    let classId: Int
    let className: String?
    let sectionId: Int?
    let sectionName: String?
    let date: String
    let isMarked: Bool
    let students: [TeacherAttendanceStudentDto]
}

struct TeacherAttendanceStudentDto: Decodable, Identifiable {
    let studentId: Int64
    let admissionNo: String?
    let studentName: String
    var status: String?     // "Present" | "Absent" | "Late" | "HalfDay" | nil
    var remarks: String?

    var id: Int64 { studentId }
}

struct TeacherSaveAttendanceRequest: Encodable {
    let classId: Int
    let sectionId: Int
    let date: String
    let entries: [TeacherAttendanceEntry]
}

struct TeacherAttendanceEntry: Encodable {
    let studentId: Int64
    let status: String
    let remarks: String?
}

// MARK: - Homework

struct TeacherHomeworkDto: Decodable, Identifiable {
    let homeworkId: Int
    let title: String
    let description: String?
    let subjectName: String?
    let className: String?
    let sectionName: String?
    let assignedDate: String?
    let dueDate: String?
    let status: String?
    let createdBy: String?

    var id: Int { homeworkId }
}

struct TeacherCreateHomeworkRequest: Encodable {
    let classId: Int
    let sectionId: Int?
    let subjectId: Int?
    let title: String
    let description: String?
    let assignedDate: String
    // Phase 93: due date retired — server no longer collects it (column made
    // nullable). Field dropped from the request entirely.
}

// MARK: - Announcements (Phase 90 — teacher class/section announcements)

// GET /mobile/teacher/announcements?classId= . Mirrors the parent AnnouncementDto
// plus the Phase 90 section fields. All optional except the id so a missing
// field never fails decode. A NULL `sectionId` means the announcement is
// class-wide (reaches every section); a set `sectionId` targets that section.
struct TeacherAnnouncementDto: Decodable, Identifiable {
    let announcementId: Int
    let title: String?
    let description: String?
    let sectionId: Int?
    let sectionName: String?
    let isPinned: Bool?
    let publishedDate: String?
    let attachmentUrl: String?

    var id: Int { announcementId }
}

// POST /mobile/teacher/announcements. The server forces scope = 'Class';
// `sectionId` nil = whole class, set = that section only.
struct TeacherCreateAnnouncementRequest: Encodable {
    let classId: Int
    let sectionId: Int?
    let title: String
    let description: String?
}
