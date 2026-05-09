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
