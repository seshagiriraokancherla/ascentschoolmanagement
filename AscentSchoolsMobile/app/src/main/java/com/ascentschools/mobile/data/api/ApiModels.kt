package com.ascentschools.mobile.data.api

// ── Generic API envelope ──────────────────────────────────────────────────────

data class ApiResponse<T>(
    val success: Boolean,
    val data: T?,
    val message: String?,
    val errors: Any?
)

// ── Auth — Teacher ────────────────────────────────────────────────────────────

data class TeacherLoginRequest(val username: String, val password: String)

data class TeacherAuthResponse(
    val accessToken: String,
    val tokenType: String,
    val fullName: String?,
    val userId: Int,
    val schoolId: Int
)

// ── Teacher Data ──────────────────────────────────────────────────────────────

data class TeacherClassDto(val classId: Int, val className: String)
data class TeacherSectionDto(val sectionId: Int, val sectionName: String)

// Mirrors AttendanceGridDto / AttendanceStudentDto from the backend
data class TeacherAttendanceGridDto(
    val classId   : Int,
    val className : String,
    val date      : String,
    val isMarked  : Boolean,
    val students  : List<TeacherStudentAttendanceDto>
)

data class TeacherStudentAttendanceDto(
    val studentId   : Long,
    val studentName : String,
    val admissionNo : String,
    val status      : String?,   // null = not yet marked
    val remarks     : String?
)

data class TeacherSaveAttendanceRequest(
    val classId   : Int,
    val sectionId : Int,
    val date      : String,
    val entries   : List<TeacherAttendanceEntry>
)

data class TeacherAttendanceEntry(
    val studentId : Long,
    val status    : String,   // "Present" | "Absent" | "Late"
    val remarks   : String? = null
)

// Matches HomeworkDto from backend (dates come as ISO strings from Gson)
data class TeacherHomeworkDto(
    val homeworkId  : Int,
    val title       : String,
    val description : String?,
    val subjectName : String?,
    val className   : String?,
    val assignedDate: String,
    val dueDate     : String,
    val status      : String?,
    val createdBy   : String?
)

data class TeacherCreateHomeworkRequest(
    val classId     : Int,
    val title       : String,
    val description : String?,
    val assignedDate: String,
    val dueDate     : String
)

// ── Auth — Parent ─────────────────────────────────────────────────────────────

data class ParentRegisterRequest(val fullName: String, val mobile: String,
                                 val pin: String, val email: String? = null)
data class ParentLoginRequest(val identifier: String, val pin: String)
data class SmsOtpRequest(val mobile: String, val deviceId: String)
data class SmsVerifyOtpRequest(val mobile: String, val otp: String, val deviceId: String)
data class SelectChildRequest(val linkId: Int)

// ── Auth — Response ───────────────────────────────────────────────────────────
// Matches MobileAuthResponse on backend

data class AuthResponse(
    val accessToken: String,
    val tokenType: String,
    val fullName: String?,
    val className: String?,
    val sectionName: String?,
    val admissionNo: String?,
    val studentId: Long?,
    val parentId: Int?
)

// Matches ChildDto on backend
data class ChildDto(
    val linkId: Int,
    val studentId: Long,
    val studentName: String,
    val className: String,
    val sectionName: String?,
    val admissionNo: String,
    val schoolCode: String,
    val isActive: Boolean
)

// ── Student Profile ───────────────────────────────────────────────────────────
// Matches StudentProfileDto on backend (fullName, not studentName)

data class StudentProfileDto(
    val studentId: Long,
    val admissionNo: String,
    val fullName: String,
    val className: String,
    val sectionName: String?,
    val dateOfBirth: String?,
    val gender: String?,
    val bloodGroup: String?,
    val fatherName: String?,
    val motherName: String?,
    val mobile: String?,
    val email: String?,
    val address: String?,
    val photoPath: String?,
    val academicYear: String?
)

// ── Attendance ────────────────────────────────────────────────────────────────
// Matches AttendanceSummaryDto on backend (totalDays, not totalWorkingDays)

data class AttendanceSummaryDto(
    val totalDays: Int,
    val presentDays: Int,
    val absentDays: Int,
    val lateDays: Int,
    val records: List<AttendanceRecordDto>
)

data class AttendanceRecordDto(
    val date: String,
    val status: String,
    val remarks: String?
)

// ── Marks ─────────────────────────────────────────────────────────────────────
// Matches backend: MarksResultDto → ExamResultDto (field "marks") → SubjectMarkDto

data class MarksResultDto(
    val academicYearId: Int,
    val academicYearName: String?,
    val exams: List<ExamResultDto>
)

data class ExamResultDto(
    val examTypeId: Int,
    val examTypeName: String,
    val marks: List<SubjectMarkDto>,   // backend field name is "marks"
    val totalObtained: Double,
    val totalMax: Double,
    val percentage: Double
)

data class SubjectMarkDto(
    val subjectName: String,
    val marksObtained: Double,
    val maxMarks: Double,
    val isAbsent: Boolean
)

// ── Homework ──────────────────────────────────────────────────────────────────
// Matches HomeworkDto on backend; assignedDate/dueDate serialised as ISO strings via Gson

data class HomeworkDto(
    val homeworkId    : Int,
    val title         : String?,
    val description   : String?,
    val subjectName   : String?,
    val assignedDate  : String?,
    val dueDate       : String?,
    val attachmentUrl : String?,
    val attachments   : List<AttachmentDto>?
)

data class AttachmentDto(
    val attachmentId: Int,
    val fileName: String,
    val filePath: String,
    val fileSizeKb: Int?
)

// ── Announcements ─────────────────────────────────────────────────────────────
// Matches AnnouncementDto on backend (description, createdAt)

data class AnnouncementDto(
    val announcementId : Int,
    val title          : String?,
    val description    : String?,
    val scope          : String?,
    val isPinned       : Boolean,
    val attachmentUrl  : String?,   // optional PDF/doc URL
    val createdAt      : String?
)

// ── Events ────────────────────────────────────────────────────────────────────

data class SchoolEventDto(
    val eventId       : Int,
    val title         : String?,
    val description   : String?,
    val eventDate     : String?,   // ISO date: "2025-08-15"
    val mediaType     : String?,   // "image" | "video"
    val mediaUrl      : String?,
    val thumbnailUrl  : String?,
    val attachmentUrl : String?,   // optional PDF/doc URL
    val scope         : String?,
    val isPinned      : Boolean
)

// ── Fee ───────────────────────────────────────────────────────────────────────
// Wrapper for GET /mobile/fees/outstanding — API returns object, years list lives inside
data class CrossYearFeeSummaryDto(
    val years: List<MobileFeeSummaryDto> = emptyList()
)

// One row per academic year. Field names match server's YearFeeSummaryDto (camelCase).
data class MobileFeeSummaryDto(
    val academicYearId                               : Int?,
    val academicYear                                 : String?,
    @com.google.gson.annotations.SerializedName("totalStructure")
    val totalAmount                                  : Double = 0.0,
    @com.google.gson.annotations.SerializedName("totalPaid")
    val paidAmount                                   : Double = 0.0,
    @com.google.gson.annotations.SerializedName("totalOutstanding")
    val outstandingAmount                            : Double = 0.0,
    val lineItems                                    : List<MobileFeeLineItemDto> = emptyList()
)

data class MobileFeeLineItemDto(
    val feeTypeId        : Int?,
    val feeTypeName      : String?,
    val termId           : Int?,
    val termName         : String?,
    val feePeriodId      : Int?,
    val busRouteId       : Int?,
    val hostelId         : Int?,
    @com.google.gson.annotations.SerializedName("structureAmount")
    val amount           : Double = 0.0,
    val paidAmount       : Double = 0.0,
    val outstanding      : Double = 0.0,
    val concessionAmount : Double = 0.0,
) {
    // isPaid not returned by server — derived locally
    val isPaid: Boolean get() = outstanding <= 0
}

// feeTypeCategory replaces paymentModeId — server resolves the online payment mode
data class MobileCreateOrderRequest(
    val academicYearId  : Int,
    val feeTypeCategory : String,
    val items           : List<MobileFeeOrderItem>
)

data class MobileFeeOrderItem(
    val feeTypeId        : Int?,
    val termId           : Int?,
    val feePeriodId      : Int?,
    val busRouteId       : Int?,
    val hostelId         : Int?,
    val amount           : Double,
    val concessionAmount : Double = 0.0
)

data class MobileOrderResponse(
    val gatewayOrderId  : Int,
    val externalOrderId : String,
    val keyId           : String,
    val amountInPaise   : Long,
    val currency        : String,
    val gatewayName     : String = "Razorpay"
)

data class MobileVerifyRequest(
    val gatewayOrderId : Int,
    val paymentId      : String,
    val orderId        : String,
    val signature      : String
)

data class MobilePaymentResultDto(
    val receiptId : Int,
    val receiptNo : String?,
    val amount    : Double,
    val message   : String?
)
