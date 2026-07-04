import Foundation

// MARK: - Student Profile

struct StudentProfileDto: Decodable {
    let studentId: Int64?
    let admissionNo: String?
    let fullName: String?
    let className: String?
    let sectionName: String?
    let dateOfBirth: String?
    let gender: String?
    let bloodGroup: String?
    let fatherName: String?
    let motherName: String?
    let mobile: String?
    let email: String?
    let address: String?
    let photoPath: String?
    let academicYear: String?
}

// MARK: - Attendance

struct AttendanceSummaryDto: Decodable {
    let totalDays: Int
    let presentDays: Int
    let absentDays: Int
    let lateDays: Int
    // Phase 73 (Android parity): server sends `halfDayDays` (default 0). Optional here
    // so older API deployments that don't emit the field decode cleanly to 0.
    let halfDayDays: Int?
    let records: [AttendanceRecordDto]?

    // Attendance percentage counts Half Day as 0.5 of a Present day (Phase 73 rule):
    //     (present + 0.5 × halfDay) / total
    // Returns 0 for empty months so views never divide by zero.
    var attendancePercent: Double {
        guard totalDays > 0 else { return 0 }
        let halfDay = Double(halfDayDays ?? 0)
        let effective = Double(presentDays) + 0.5 * halfDay
        return (effective / Double(totalDays)) * 100.0
    }
}

struct AttendanceRecordDto: Decodable, Identifiable {
    let date: String
    let status: String
    let remarks: String?

    var id: String { date }
}

// MARK: - Marks

struct MarksResultDto: Decodable, Identifiable {
    let academicYearId: Int
    let academicYear: String?
    let exams: [ExamResultDto]

    var id: Int { academicYearId }
}

struct ExamResultDto: Decodable, Identifiable {
    let examTypeId: Int
    let examName: String?
    let subjects: [SubjectMarkDto]
    let totalMarks: Double?
    let obtainedMarks: Double?
    let percentage: Double?

    var id: Int { examTypeId }
}

struct SubjectMarkDto: Decodable, Identifiable {
    let subjectId: Int
    let subjectName: String?
    let maxMarks: Double?
    let obtainedMarks: Double?
    let isAbsent: Bool?
    let grade: String?

    var id: Int { subjectId }
}

// MARK: - Homework

struct HomeworkDto: Decodable, Identifiable {
    let homeworkId: Int
    let title: String?
    let description: String?
    let subjectName: String?
    let className: String?
    let sectionName: String?
    let assignedDate: String?
    let dueDate: String?
    let attachments: [HomeworkAttachmentDto]?
    let attachmentUrl: String?

    var id: Int { homeworkId }
}

struct HomeworkAttachmentDto: Decodable, Identifiable {
    let attachmentId: Int
    let fileName: String?
    let fileUrl: String?

    var id: Int { attachmentId }
}

// MARK: - Announcements

struct AnnouncementDto: Decodable, Identifiable {
    let announcementId: Int
    let title: String?
    let description: String?
    let scope: String?
    let isPinned: Bool?
    let publishedDate: String?
    let attachmentUrl: String?

    var id: Int { announcementId }
}

// MARK: - School events

struct SchoolEventDto: Decodable, Identifiable {
    let eventId: Int
    let title: String?
    let description: String?
    let eventDate: String?
    let mediaType: String?   // "image" | "video"
    let mediaUrl: String?
    let thumbnailUrl: String?
    let attachmentUrl: String?
    let isPinned: Bool?

    var id: Int { eventId }
}
