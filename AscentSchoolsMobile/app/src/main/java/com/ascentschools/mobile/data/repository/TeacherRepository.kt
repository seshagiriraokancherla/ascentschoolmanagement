package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.data.api.*
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import retrofit2.Response

class TeacherRepository(private val api: ApiService) {

    suspend fun getClasses(): Result<List<TeacherClassDto>> = runCatching {
        api.getTeacherClasses().bodyOrError().dataOrError()
    }

    suspend fun getSections(classId: Int): Result<List<TeacherSectionDto>> = runCatching {
        api.getTeacherSections(classId).bodyOrError().dataOrError()
    }

    suspend fun getAttendance(classId: Int, sectionId: Int, date: String): Result<TeacherAttendanceGridDto> = runCatching {
        api.getTeacherAttendance(classId, sectionId, date).bodyOrError().dataOrError()
    }

    suspend fun saveAttendance(request: TeacherSaveAttendanceRequest): Result<Unit> = runCatching {
        val body = api.saveTeacherAttendance(request).bodyOrError()
        if (!body.success) error(body.message ?: "Save failed")
    }

    suspend fun getHomework(classId: Int, sectionId: Int?, page: Int, pageSize: Int): Result<TeacherHomeworkPage> = runCatching {
        api.getTeacherHomework(classId, sectionId, page, pageSize).bodyOrError().dataOrError()
    }

    suspend fun createHomework(request: TeacherCreateHomeworkRequest): Result<Unit> = runCatching {
        val body = api.createTeacherHomework(request).bodyOrError()
        if (!body.success) error(body.message ?: "Create failed")
    }

    suspend fun getAnnouncements(classId: Int): Result<List<TeacherAnnouncementDto>> = runCatching {
        api.getTeacherAnnouncements(classId).bodyOrError().dataOrError()
    }

    suspend fun createAnnouncement(request: TeacherCreateAnnouncementRequest): Result<Unit> = runCatching {
        val body = api.createTeacherAnnouncement(request).bodyOrError()
        if (!body.success) error(body.message ?: "Create failed")
    }

    // ── Marks ───────────────────────────────────────────────────────────────────

    suspend fun getExamTypes(): Result<List<TeacherExamTypeDto>> = runCatching {
        api.getTeacherExamTypes().bodyOrError().dataOrError()
    }

    suspend fun getMarksSubjects(classId: Int): Result<List<TeacherMarksSubjectDto>> = runCatching {
        api.getTeacherMarksSubjects(classId).bodyOrError().dataOrError()
    }

    suspend fun getMarks(classId: Int, sectionId: Int, examTypeId: Int, subjectId: Int): Result<TeacherSubjectMarksDto> = runCatching {
        api.getTeacherMarks(classId, sectionId, examTypeId, subjectId).bodyOrError().dataOrError()
    }

    suspend fun saveMarks(request: TeacherSaveMarksRequest): Result<Unit> = runCatching {
        val body = api.saveTeacherMarks(request).bodyOrError()
        if (!body.success) error(body.message ?: "Save failed")
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // ── Messaging (teacher side — threads of their assigned classes) ──────

    suspend fun getThreads(): Result<List<MessageThreadDto>> = runCatching {
        api.getTeacherThreads().bodyOrError().dataOrError()
    }

    suspend fun getThread(threadId: Int): Result<MessageThreadDetailDto> = runCatching {
        api.getTeacherThread(threadId).bodyOrError().dataOrError()
    }

    suspend fun reply(threadId: Int, text: String): Result<Unit> = runCatching {
        val body = api.replyTeacherMessage(threadId, SendMessageRequest(text)).bodyOrError()
        if (!body.success) error(body.message ?: "Reply failed")
    }

    suspend fun markThreadRead(threadId: Int): Result<Unit> = runCatching {
        api.markTeacherThreadRead(threadId).bodyOrError()
        Unit
    }

    suspend fun reportMessage(threadId: Int, messageId: Int, reason: String?): Result<Unit> = runCatching {
        val body = api.reportTeacherMessage(threadId, ReportMessageRequest(messageId, reason)).bodyOrError()
        if (!body.success) error(body.message ?: "Report failed")
    }

    suspend fun blockThread(threadId: Int): Result<Unit> = runCatching {
        val body = api.blockTeacherThread(threadId).bodyOrError()
        if (!body.success) error(body.message ?: "Block failed")
    }

    suspend fun unblockThread(threadId: Int): Result<Unit> = runCatching {
        val body = api.unblockTeacherThread(threadId).bodyOrError()
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

    private fun <T> ApiResponse<T>.dataOrError(): T {
        if (!success || data == null) error(message ?: "No data")
        return data
    }
}
