package com.ascentschools.mobile.data.api

// ── Generic API envelope ──────────────────────────────────────────────────────

data class ApiResponse<T>(
    val success: Boolean,
    val data: T?,
    val message: String?,
    val errors: Any?
)

// ── Single-app onboarding + branding ────────────────────────────────────────────

// GET mobile/auth/school-by-code?code=1001
data class SchoolByCodeDto(
    val schoolCode: String?,   // subdomain — stored as X-School-Code
    val name: String?          // school/group name shown for confirmation
)

// GET branding  (only the fields the app uses; Gson ignores the rest)
data class BrandingDto(
    val displayName: String?,
    val logoPath: String?,     // relative (/Uploads/...) — resolve with RetrofitClient.mediaBaseUrl
    val primaryColor: String? = null   // e.g. "#1E3A8A" — used by the tile dashboard band/tint
)

// ── Auth — Teacher ────────────────────────────────────────────────────────────

data class TeacherLoginRequest(val username: String, val password: String)

data class TeacherAuthResponse(
    val accessToken: String,
    val refreshToken: String?,
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
    val dueDate     : String?,   // retired — null for new homework
    val status      : String?,
    val createdBy   : String?
)

data class TeacherCreateHomeworkRequest(
    val classId     : Int,
    val title       : String,
    val description : String?,
    val assignedDate: String
)

// Mirrors School AnnouncementDto from backend (createdAt is an ISO string from Gson)
data class TeacherAnnouncementDto(
    val announcementId : Int,
    val title          : String,
    val description    : String?,
    val scope          : String?,
    val classId        : Int?,
    val className      : String?,
    val sectionId      : Int?,
    val sectionName    : String?,
    val isPinned       : Boolean,
    val createdBy      : String?,
    val createdAt      : String
)

data class TeacherCreateAnnouncementRequest(
    val classId     : Int,
    val sectionId   : Int?,   // null = whole class
    val title       : String,
    val description : String?
)

// ── Push notifications (FCM) ────────────────────────────────────────────────────

data class RegisterPushTokenRequest(
    val fcmToken      : String,
    val applicationId : String,
    val platform      : String = "android"
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
    val refreshToken: String?,
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
    val halfDayDays: Int = 0,
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
    val attachments   : List<AttachmentDto>?,
    val media         : List<MediaUploadDto>? = null   // R2 uploads (Phase B)
)

data class AttachmentDto(
    val attachmentId: Int,
    val fileName: String,
    val filePath: String,
    val fileSizeKb: Int?
)

// R2-uploaded file for homework / announcement / event
data class MediaUploadDto(
    val uploadId  : Long,
    val fileName  : String?,
    val fileUrl   : String?,
    val fileType  : String?,   // image | doc | audio | video
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
    val attachmentUrl  : String?,   // optional PDF/doc URL (legacy)
    val createdAt      : String?,
    val media          : List<MediaUploadDto>? = null   // R2 uploads (Phase B)
)

// ── Events ────────────────────────────────────────────────────────────────────

data class SchoolEventDto(
    val eventId       : Int,
    val title         : String?,
    val description   : String?,
    val eventDate     : String?,   // ISO date: "2025-08-15"
    val mediaType     : String?,   // "image" | "video"
    val mediaUrl      : String?,   // optional YouTube URL
    val thumbnailUrl  : String?,
    val attachmentUrl : String?,   // optional PDF/doc URL (legacy)
    val scope         : String?,
    val isPinned      : Boolean,
    val media         : List<MediaUploadDto>? = null   // R2 uploads (Phase B)
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
    val periodLabel      : String? = null,   // set for Monthly items (term_id null)
    val busRouteId       : Int?,
    val hostelId         : Int?,
    @com.google.gson.annotations.SerializedName("structureAmount")
    val amount           : Double = 0.0,
    val paidAmount       : Double = 0.0,
    val outstanding      : Double = 0.0,
    val concessionAmount : Double = 0.0,
    val receiptId        : Int?   = null,   // the receipt that paid this line (null if unpaid)
    val createdBy        : String? = null,  // receipt's created_by, e.g. "Mobile App"
) {
    // isPaid not returned by server — derived locally
    val isPaid: Boolean get() = outstanding <= 0
    // Show the Print button only for paid lines created via the mobile app.
    val canPrint: Boolean get() = isPaid && receiptId != null && createdBy == "Mobile App"
}

// ── Receipt detail (for in-app print / save-as-PDF) ──────────────────────────
data class ReceiptDetailDto(
    val receiptId       : Int = 0,
    val receiptNo       : String? = null,
    val studentName     : String? = null,
    val admissionNo     : String? = null,
    val className       : String? = null,
    val fatherName      : String? = null,
    val academicYear    : String? = null,
    val paymentDate     : String? = null,
    val totalAmount     : Double = 0.0,
    val paymentModeName : String? = null,
    val status          : String? = null,
    val remarks         : String? = null,
    val createdBy       : String? = null,
    val items           : List<ReceiptItemDto> = emptyList()
)

data class ReceiptItemDto(
    val feeTypeName      : String? = null,
    val routeName        : String? = null,
    val hostelName       : String? = null,
    val termName         : String? = null,
    val amount           : Double = 0.0,
    val concessionAmount : Double = 0.0,
    val netAmount        : Double = 0.0
)

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
    val receiptId   : Int,
    val receiptNo   : String?,
    // Server returns the full receipt whose total field is "totalAmount"
    // (FeeReceiptDto.TotalAmount). Must match that JSON name or it stays 0.0.
    val totalAmount : Double = 0.0,
    val message     : String?
)

// ── App update gate ─────────────────────────────────────────────────────────
// GET mobile/app/config?applicationId=&platform=android&versionCode=
data class AppVersionStatusDto(
    val updateRequired          : Boolean = false,  // true → hard block
    val updateAvailable         : Boolean = false,  // true → soft nudge
    val minSupportedVersionCode : Int     = 0,
    val latestVersionCode       : Int     = 0,
    val message                 : String? = null,
    val storeUrl                : String? = null
)

// ── Messaging (parent <-> teacher) ─────────────────────────────────────────

data class MessageDto(
    val messageId  : Int,
    val threadId   : Int,
    val senderType : String,   // parent | teacher
    val senderId   : Int,
    val senderName : String?,
    val body       : String,
    val status     : String,   // Active | Removed
    val readAt     : String?,
    val createdAt  : String
)

/** Parent chat: availability + who it reaches + the conversation, in one call. */
data class ParentThreadViewDto(
    val canMessage    : Boolean,
    val reason        : String?,        // why not, when canMessage = false
    val teachers      : List<String>?,  // recipient display names
    val threadId      : Int?,           // null until the first message
    val status        : String?,        // Active | Blocked
    val blockedByType : String?,        // parent | teacher
    val messages      : List<MessageDto>?
)

/** One conversation in the teacher's thread list. */
data class MessageThreadDto(
    val threadId        : Int,
    val studentUniqueId : Int,
    val parentId        : Int,
    val studentName     : String?,
    val admissionNo     : String?,
    val className       : String?,
    val sectionName     : String?,
    val status          : String?,
    val blockedByType   : String?,
    val blockedAt       : String?,
    val lastMessageBody : String?,
    val lastMessageAt   : String?,
    val unreadCount     : Int,
    val createdAt       : String
)

data class MessageThreadDetailDto(
    val thread   : MessageThreadDto?,
    val messages : List<MessageDto>?
)

data class SendMessageRequest(val body: String)

data class ReportMessageRequest(
    val messageId : Int,
    val reason    : String?
)
