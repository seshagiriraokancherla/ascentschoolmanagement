package com.ascentschools.mobile.data.api

import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // ── Single-app onboarding + branding ───────────────────────────────────────

    @GET("mobile/auth/school-by-code")
    suspend fun getSchoolByCode(@Query("code") code: String): Response<ApiResponse<SchoolByCodeDto>>

    @GET("branding")
    suspend fun getBranding(): Response<ApiResponse<BrandingDto>>

    // ── App update gate (force / soft update) ──────────────────────────────────
    @GET("mobile/app/config")
    suspend fun getAppConfig(
        @Query("applicationId") applicationId: String,
        @Query("platform")      platform:      String,
        @Query("versionCode")   versionCode:   Int
    ): Response<ApiResponse<AppVersionStatusDto>>

    // ── Teacher Auth ──────────────────────────────────────────────────────────

    @POST("mobile/auth/teacher/login")
    suspend fun loginTeacher(@Body request: TeacherLoginRequest): Response<ApiResponse<TeacherAuthResponse>>

    @POST("mobile/auth/teacher/refresh")
    suspend fun refreshTeacherToken(): Response<ApiResponse<TeacherAuthResponse>>

    @POST("mobile/auth/teacher/logout")
    suspend fun logoutTeacher(): Response<ApiResponse<Any>>

    // ── Teacher Data ──────────────────────────────────────────────────────────

    @GET("mobile/teacher/classes")
    suspend fun getTeacherClasses(): Response<ApiResponse<List<TeacherClassDto>>>

    @GET("mobile/teacher/sections")
    suspend fun getTeacherSections(@Query("classId") classId: Int): Response<ApiResponse<List<TeacherSectionDto>>>

    @GET("mobile/teacher/attendance")
    suspend fun getTeacherAttendance(
        @Query("classId")   classId:   Int,
        @Query("sectionId") sectionId: Int,
        @Query("date")      date:      String
    ): Response<ApiResponse<TeacherAttendanceGridDto>>

    @POST("mobile/teacher/attendance")
    suspend fun saveTeacherAttendance(@Body request: TeacherSaveAttendanceRequest): Response<ApiResponse<Any>>

    @GET("mobile/teacher/homework")
    suspend fun getTeacherHomework(@Query("classId") classId: Int): Response<ApiResponse<List<TeacherHomeworkDto>>>

    @POST("mobile/teacher/homework")
    suspend fun createTeacherHomework(@Body request: TeacherCreateHomeworkRequest): Response<ApiResponse<Any>>

    // ── Parent Auth ───────────────────────────────────────────────────────────

    @POST("mobile/auth/parent/request-otp")
    suspend fun requestParentOtp(@Body request: SmsOtpRequest): Response<ApiResponse<Any>>

    @POST("mobile/auth/parent/verify-otp")
    suspend fun verifyParentOtp(@Body request: SmsVerifyOtpRequest): Response<ApiResponse<AuthResponse>>

    @POST("mobile/auth/parent/register")
    suspend fun registerParent(@Body request: ParentRegisterRequest): Response<ApiResponse<AuthResponse>>

    @POST("mobile/auth/parent/login")
    suspend fun loginParent(@Body request: ParentLoginRequest): Response<ApiResponse<AuthResponse>>

    @POST("mobile/auth/parent/refresh")
    suspend fun refreshParentToken(): Response<ApiResponse<AuthResponse>>

    @POST("mobile/auth/parent/logout")
    suspend fun logoutParent(): Response<ApiResponse<Any>>

    @GET("mobile/auth/parent/children")
    suspend fun getChildren(): Response<ApiResponse<List<ChildDto>>>

    @POST("mobile/auth/parent/select-child")
    suspend fun selectChild(@Body request: SelectChildRequest): Response<ApiResponse<AuthResponse>>

    // ── Student Data ──────────────────────────────────────────────────────────

    @GET("mobile/student/profile")
    suspend fun getProfile(): Response<ApiResponse<StudentProfileDto>>

    @GET("mobile/student/attendance")
    suspend fun getAttendance(
        @Query("month") month: Int,
        @Query("year")  year: Int
    ): Response<ApiResponse<AttendanceSummaryDto>>

    @GET("mobile/student/marks")
    suspend fun getMarks(
        @Query("academicYearId") academicYearId: Int
    ): Response<ApiResponse<List<MarksResultDto>>>

    @GET("mobile/student/homework")
    suspend fun getHomework(): Response<ApiResponse<List<HomeworkDto>>>

    @GET("mobile/student/announcements")
    suspend fun getAnnouncements(): Response<ApiResponse<List<AnnouncementDto>>>

    @GET("mobile/student/events")
    suspend fun getEvents(): Response<ApiResponse<List<SchoolEventDto>>>

    // ── Fees (parent portal endpoints) ───────────────────────────────────────

    @GET("mobile/fees/outstanding")
    suspend fun getOutstanding(
        @Query("feeTypeCategory") feeTypeCategory: String
    ): Response<ApiResponse<CrossYearFeeSummaryDto>>

    @POST("mobile/fees/payment-orders")
    suspend fun createPaymentOrder(
        @Body request: MobileCreateOrderRequest
    ): Response<ApiResponse<MobileOrderResponse>>

    @POST("mobile/fees/payment-orders/{gatewayOrderId}/verify")
    suspend fun verifyPayment(
        @Path("gatewayOrderId") gatewayOrderId: Int,
        @Body request: MobileVerifyRequest
    ): Response<ApiResponse<MobilePaymentResultDto>>

    @GET("mobile/fees/receipts/{id}")
    suspend fun getReceiptDetail(
        @Path("id") id: Int
    ): Response<ApiResponse<ReceiptDetailDto>>
}
