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

    suspend fun getHomework(classId: Int): Result<List<TeacherHomeworkDto>> = runCatching {
        api.getTeacherHomework(classId).bodyOrError().dataOrError()
    }

    suspend fun createHomework(request: TeacherCreateHomeworkRequest): Result<Unit> = runCatching {
        val body = api.createTeacherHomework(request).bodyOrError()
        if (!body.success) error(body.message ?: "Create failed")
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
