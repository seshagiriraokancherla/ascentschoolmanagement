import Foundation

// All routes mirror the Android `ApiService` interface and the
// "Mobile App API Endpoints" table in /CLAUDE.md.

extension APIClient {

    // MARK: - Parent auth (SMS OTP + legacy PIN)

    func requestParentOtp(mobile: String, deviceId: String) async throws {
        try await sendVoid(
            "mobile/auth/parent/request-otp",
            method: .post,
            body: SmsOtpRequest(mobile: mobile, deviceId: deviceId)
        )
    }

    func verifyParentOtp(mobile: String, otp: String, deviceId: String) async throws -> AuthResponse {
        try await send(
            "mobile/auth/parent/verify-otp",
            method: .post,
            body: SmsVerifyOtpRequest(mobile: mobile, otp: otp, deviceId: deviceId)
        )
    }

    func registerParent(_ request: ParentRegisterRequest) async throws -> AuthResponse {
        try await send("mobile/auth/parent/register", method: .post, body: request)
    }

    func loginParent(_ request: ParentLoginRequest) async throws -> AuthResponse {
        try await send("mobile/auth/parent/login", method: .post, body: request)
    }

    func refreshParent() async throws -> AuthResponse {
        try await send("mobile/auth/parent/refresh", method: .post)
    }

    func logoutParent() async throws {
        try await sendVoid("mobile/auth/parent/logout", method: .post)
    }

    func parentChildren() async throws -> [ChildDto] {
        try await send("mobile/auth/parent/children")
    }

    func selectChild(linkId: Int) async throws -> AuthResponse {
        try await send(
            "mobile/auth/parent/select-child",
            method: .post,
            body: SelectChildRequest(linkId: linkId)
        )
    }

    // MARK: - Teacher auth

    func loginTeacher(_ request: TeacherLoginRequest) async throws -> TeacherAuthResponse {
        try await send("mobile/auth/teacher/login", method: .post, body: request)
    }

    func refreshTeacher() async throws -> TeacherAuthResponse {
        try await send("mobile/auth/teacher/refresh", method: .post)
    }

    func logoutTeacher() async throws {
        try await sendVoid("mobile/auth/teacher/logout", method: .post)
    }

    // MARK: - Student data (parent or student JWT with child context)

    func studentProfile() async throws -> StudentProfileDto {
        try await send("mobile/student/profile")
    }

    func studentAttendance(month: Int, year: Int) async throws -> AttendanceSummaryDto {
        try await send(
            "mobile/student/attendance",
            query: [
                URLQueryItem(name: "month", value: String(month)),
                URLQueryItem(name: "year", value: String(year)),
            ]
        )
    }

    func studentMarks(academicYearId: Int? = nil) async throws -> [MarksResultDto] {
        var query: [URLQueryItem] = []
        if let id = academicYearId {
            query.append(URLQueryItem(name: "academicYearId", value: String(id)))
        }
        return try await send("mobile/student/marks", query: query)
    }

    func studentHomework() async throws -> [HomeworkDto] {
        try await send("mobile/student/homework")
    }

    func studentAnnouncements() async throws -> [AnnouncementDto] {
        try await send("mobile/student/announcements")
    }

    func studentEvents() async throws -> [SchoolEventDto] {
        try await send("mobile/student/events")
    }

    // MARK: - Teacher data

    func teacherClasses() async throws -> [TeacherClassDto] {
        try await send("mobile/teacher/classes")
    }

    func teacherSections(classId: Int) async throws -> [TeacherSectionDto] {
        try await send(
            "mobile/teacher/sections",
            query: [URLQueryItem(name: "classId", value: String(classId))]
        )
    }

    func teacherAttendance(classId: Int, sectionId: Int, date: String) async throws -> TeacherAttendanceGridDto {
        try await send(
            "mobile/teacher/attendance",
            query: [
                URLQueryItem(name: "classId", value: String(classId)),
                URLQueryItem(name: "sectionId", value: String(sectionId)),
                URLQueryItem(name: "date", value: date),
            ]
        )
    }

    func saveTeacherAttendance(_ request: TeacherSaveAttendanceRequest) async throws {
        try await sendVoid("mobile/teacher/attendance", method: .post, body: request)
    }

    func teacherHomework(classId: Int) async throws -> [TeacherHomeworkDto] {
        try await send(
            "mobile/teacher/homework",
            query: [URLQueryItem(name: "classId", value: String(classId))]
        )
    }

    func createTeacherHomework(_ request: TeacherCreateHomeworkRequest) async throws -> TeacherHomeworkDto {
        try await send("mobile/teacher/homework", method: .post, body: request)
    }

    // MARK: - Messaging (Phase 92)

    // Parent side (child context). One thread per selected child — no list.
    func parentThread() async throws -> ParentThreadViewDto {
        try await send("mobile/messages")
    }

    func sendParentMessage(body: String) async throws -> SendMessageResult {
        try await send("mobile/messages", method: .post, body: SendMessageRequest(body: body))
    }

    func markParentThreadRead() async throws {
        try await sendVoid("mobile/messages/read", method: .post)
    }

    func reportParentMessage(messageId: Int, reason: String?) async throws {
        try await sendVoid(
            "mobile/messages/report",
            method: .post,
            body: ReportMessageRequest(messageId: messageId, reason: reason)
        )
    }

    func blockParentThread() async throws {
        try await sendVoid("mobile/messages/block", method: .post)
    }

    func unblockParentThread() async throws {
        try await sendVoid("mobile/messages/unblock", method: .post)
    }

    // Teacher side. Inbox spans all assigned classes; chat is per threadId.
    func teacherThreads() async throws -> [MessageThreadDto] {
        try await send("mobile/teacher/messages")
    }

    func teacherThread(threadId: Int) async throws -> MessageThreadDetailDto {
        try await send("mobile/teacher/messages/\(threadId)")
    }

    func replyTeacherMessage(threadId: Int, body: String) async throws -> SendMessageResult {
        try await send(
            "mobile/teacher/messages/\(threadId)",
            method: .post,
            body: SendMessageRequest(body: body)
        )
    }

    func markTeacherThreadRead(threadId: Int) async throws {
        try await sendVoid("mobile/teacher/messages/\(threadId)/read", method: .post)
    }

    func reportTeacherMessage(threadId: Int, messageId: Int, reason: String?) async throws {
        try await sendVoid(
            "mobile/teacher/messages/\(threadId)/report",
            method: .post,
            body: ReportMessageRequest(messageId: messageId, reason: reason)
        )
    }

    func blockTeacherThread(threadId: Int) async throws {
        try await sendVoid("mobile/teacher/messages/\(threadId)/block", method: .post)
    }

    func unblockTeacherThread(threadId: Int) async throws {
        try await sendVoid("mobile/teacher/messages/\(threadId)/unblock", method: .post)
    }

    // Phase 90 — teacher class/section announcements
    func teacherAnnouncements(classId: Int) async throws -> [TeacherAnnouncementDto] {
        try await send(
            "mobile/teacher/announcements",
            query: [URLQueryItem(name: "classId", value: String(classId))]
        )
    }

    func createTeacherAnnouncement(_ request: TeacherCreateAnnouncementRequest) async throws -> TeacherAnnouncementDto {
        try await send("mobile/teacher/announcements", method: .post, body: request)
    }

    // MARK: - Fees (parent JWT with child context)

    func feeOutstanding(category: FeeTypeCategory) async throws -> CrossYearFeeSummaryDto {
        try await send(
            "mobile/fees/outstanding",
            query: [URLQueryItem(name: "feeTypeCategory", value: category.rawValue)]
        )
    }

    func feeGatewayConfig() async throws -> GatewayConfigDto {
        try await send("mobile/fees/gateway-config")
    }

    func createPaymentOrder(_ request: MobileCreateOrderRequest) async throws -> MobileOrderResponse {
        try await send("mobile/fees/payment-orders", method: .post, body: request)
    }

    func verifyPayment(gatewayOrderId: Int, request: MobileVerifyRequest) async throws -> MobilePaymentResultDto {
        try await send(
            "mobile/fees/payment-orders/\(gatewayOrderId)/verify",
            method: .post,
            body: request
        )
    }

    // Phase 58: full receipt detail for in-app print. Server validates the
    // receipt belongs to the selected child via student_unique_id.
    func mobileReceipt(id: Int) async throws -> MobileReceiptDto {
        try await send("mobile/fees/receipts/\(id)")
    }

    // MARK: - Generic-flavor school selection (public — Phase 44)

    // Resolves the 4-digit `login_code` shared by the school office into the
    // school's subdomain (which we then persist and use as X-School-Code /
    // X-Subdomain for every subsequent request). AllowAnonymous on the server.
    func schoolByCode(code: String) async throws -> SchoolByCodeDto {
        try await send(
            "mobile/auth/school-by-code",
            query: [URLQueryItem(name: "code", value: code)]
        )
    }

    // Public branding — logo + colours + display name for the resolved school.
    // The X-Subdomain header (set by APIClient from the stored schoolCode) is
    // how the server picks the right tenant.
    func branding() async throws -> BrandingDto {
        try await send("branding")
    }

    // MARK: - App config (public — Phase 57 / 71)

    // AllowAnonymous on the server, so this works before any login. Called
    // from RootView on cold start (post silent refresh) to gate on min/latest
    // versionCode. Fails open — callers should silently ignore any error and
    // proceed as if no update was requested.
    func appConfig(applicationId: String, versionCode: Int) async throws -> AppVersionStatusDto {
        try await send(
            "mobile/app/config",
            query: [
                URLQueryItem(name: "applicationId", value: applicationId),
                URLQueryItem(name: "platform", value: "ios"),
                URLQueryItem(name: "versionCode", value: String(versionCode)),
            ]
        )
    }
}
