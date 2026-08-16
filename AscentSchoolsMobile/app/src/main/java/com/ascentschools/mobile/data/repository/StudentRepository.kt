package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.data.api.*
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import retrofit2.Response

class StudentRepository(private val api: ApiService) {

    suspend fun getProfile(): Result<StudentProfileDto> = runCatching {
        val body = api.getProfile().bodyOrError()
        if (!body.success || body.data == null) error(body.message ?: "Failed to load profile")
        body.data
    }

    suspend fun getAttendance(month: Int, year: Int): Result<AttendanceSummaryDto> = runCatching {
        val body = api.getAttendance(month, year).bodyOrError()
        if (!body.success || body.data == null) error(body.message ?: "Failed to load attendance")
        body.data
    }

    suspend fun getMarks(academicYearId: Int): Result<List<MarksResultDto>> = runCatching {
        val body = api.getMarks(academicYearId).bodyOrError()
        if (!body.success) error(body.message ?: "Failed to load marks")
        body.data ?: emptyList()
    }

    suspend fun getHomework(): Result<List<HomeworkDto>> = runCatching {
        val body = api.getHomework().bodyOrError()
        if (!body.success) error(body.message ?: "Failed to load homework")
        body.data ?: emptyList()
    }

    suspend fun getAnnouncements(): Result<List<AnnouncementDto>> = runCatching {
        val body = api.getAnnouncements().bodyOrError()
        if (!body.success) error(body.message ?: "Failed to load announcements")
        body.data ?: emptyList()
    }

    suspend fun getEvents(): Result<List<SchoolEventDto>> = runCatching {
        val body = api.getEvents().bodyOrError()
        if (!body.success) error(body.message ?: "Failed to load events")
        body.data ?: emptyList()
    }

    suspend fun getCalendar(month: Int, year: Int): Result<List<CalendarEventDto>> = runCatching {
        val body = api.getCalendar(month, year).bodyOrError()
        if (!body.success) error(body.message ?: "Failed to load calendar")
        body.data ?: emptyList()
    }

    // ── Messaging (parent side — scoped to the selected child) ────────────

    suspend fun getMessageThread(): Result<ParentThreadViewDto> = runCatching {
        val body = api.getParentThread().bodyOrError()
        if (!body.success || body.data == null) error(body.message ?: "Failed to load messages")
        body.data
    }

    suspend fun sendMessage(text: String): Result<Unit> = runCatching {
        val body = api.sendParentMessage(SendMessageRequest(text)).bodyOrError()
        if (!body.success) error(body.message ?: "Send failed")
    }

    suspend fun markMessagesRead(): Result<Unit> = runCatching {
        api.markParentThreadRead().bodyOrError()
        Unit
    }

    suspend fun reportMessage(messageId: Int, reason: String?): Result<Unit> = runCatching {
        val body = api.reportParentMessage(ReportMessageRequest(messageId, reason)).bodyOrError()
        if (!body.success) error(body.message ?: "Report failed")
    }

    suspend fun blockMessageThread(): Result<Unit> = runCatching {
        val body = api.blockParentThread().bodyOrError()
        if (!body.success) error(body.message ?: "Block failed")
    }

    suspend fun unblockMessageThread(): Result<Unit> = runCatching {
        val body = api.unblockParentThread().bodyOrError()
        if (!body.success) error(body.message ?: "Unblock failed")
    }

    private fun <T> Response<T>.bodyOrError(): T {
        if (isSuccessful) return body() ?: error("Empty response body")
        val errJson = errorBody()?.string()
        val errMsg  = try {
            val type = object : TypeToken<ApiResponse<Any>>() {}.type
            Gson().fromJson<ApiResponse<Any>>(errJson, type)?.message
        } catch (_: Exception) { null }
        error(errMsg ?: "Server error ${code()}")
    }
}
