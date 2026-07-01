package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.api.*
import com.ascentschools.mobile.data.local.TokenStore
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import retrofit2.Response

class AuthRepository(
    private val api: ApiService,
    private val tokenStore: TokenStore
) {

    // ── Single-app onboarding + branding ───────────────────────────────────────

    /** Resolve a 4-digit school code → school (subdomain + name) for the generic app. */
    suspend fun getSchoolByCode(code: String): Result<SchoolByCodeDto> {
        return runCatching {
            val resp = api.getSchoolByCode(code.trim())
            val body = resp.bodyOrError()
            if (!body.success || body.data?.schoolCode == null) error(body.message ?: "Invalid school code")
            body.data
        }
    }

    /** Load branding for the current school code; caches name + absolute logo URL. */
    suspend fun loadBranding(): Result<BrandingDto> {
        return runCatching {
            val resp = api.getBranding()
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Failed to load branding")
            tokenStore.brandingName    = body.data.displayName
            tokenStore.brandingLogoUrl = body.data.logoPath?.let { resolveMedia(it) }
            body.data
        }
    }

    private fun resolveMedia(path: String): String =
        if (path.startsWith("http", ignoreCase = true)) path
        else RetrofitClient.mediaBaseUrl + path.trimStart('/')

    // ── Teacher login ─────────────────────────────────────────────────────────

    suspend fun loginTeacher(username: String, password: String): Result<TeacherAuthResponse> {
        return runCatching {
            val resp = api.loginTeacher(TeacherLoginRequest(username.trim(), password))
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Login failed")
            tokenStore.accessToken = body.data.accessToken
            tokenStore.tokenType   = body.data.tokenType
            tokenStore.studentName = body.data.fullName
            tokenStore.userType    = "teacher"
            body.data
        }
    }

    suspend fun logoutTeacher() {
        runCatching { api.logoutTeacher() }
        tokenStore.clear()
    }

    // ── Parent SMS OTP auth ───────────────────────────────────────────────────

    suspend fun requestOtp(mobile: String): Result<Unit> {
        return runCatching {
            val resp = api.requestParentOtp(SmsOtpRequest(mobile.trim(), tokenStore.deviceId))
            val body = resp.bodyOrError()
            if (!body.success) error(body.message ?: "Failed to send OTP")
        }
    }

    suspend fun verifyOtp(mobile: String, otp: String): Result<AuthResponse> {
        return runCatching {
            val resp = api.verifyParentOtp(SmsVerifyOtpRequest(mobile.trim(), otp.trim(), tokenStore.deviceId))
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Invalid or expired OTP")
            tokenStore.accessToken = body.data.accessToken
            tokenStore.tokenType   = body.data.tokenType
            tokenStore.studentName = body.data.fullName
            tokenStore.userType    = "parent"
            body.data
        }
    }

    // ── Parent login / register ───────────────────────────────────────────────

    suspend fun loginParent(mobile: String, pin: String): Result<AuthResponse> {
        return runCatching {
            val resp = api.loginParent(ParentLoginRequest(identifier = mobile, pin = pin))
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Login failed")
            tokenStore.accessToken = body.data.accessToken
            tokenStore.tokenType   = body.data.tokenType
            tokenStore.studentName = body.data.fullName
            body.data
        }
    }

    suspend fun registerParent(fullName: String, mobile: String, pin: String): Result<AuthResponse> {
        return runCatching {
            val resp = api.registerParent(ParentRegisterRequest(fullName, mobile, pin))
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Registration failed")
            tokenStore.accessToken = body.data.accessToken
            tokenStore.tokenType   = body.data.tokenType
            body.data
        }
    }

    suspend fun getChildren(): Result<List<ChildDto>> {
        return runCatching {
            val resp = api.getChildren()
            val body = resp.bodyOrError()
            if (!body.success) error(body.message ?: "Failed to load children")
            body.data ?: emptyList()
        }
    }

    suspend fun selectChild(linkId: Int): Result<AuthResponse> {
        return runCatching {
            val resp = api.selectChild(SelectChildRequest(linkId))
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Failed to select child")
            saveSession(body.data)
            tokenStore.childLinkId = linkId   // remember so we can restore child context after a refresh
            body.data
        }
    }

    suspend fun logoutParent() {
        runCatching { api.logoutParent() }
        tokenStore.clear()
    }

    // ── Silent token refresh (called on cold start) ───────────────────────────

    suspend fun silentRefresh(): Result<Unit> {
        return runCatching {
            val resp = api.refreshParentToken()
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Session expired")
            tokenStore.accessToken = body.data.accessToken
            tokenStore.tokenType   = body.data.tokenType
            body.data.fullName?.let { tokenStore.studentName = it }
        }
    }

    suspend fun silentRefreshTeacher(): Result<Unit> {
        return runCatching {
            val resp = api.refreshTeacherToken()
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Session expired")
            tokenStore.accessToken = body.data.accessToken
            body.data.fullName?.let { tokenStore.studentName = it }
        }
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

    private fun saveSession(data: AuthResponse) {
        tokenStore.accessToken = data.accessToken
        tokenStore.tokenType   = data.tokenType
        tokenStore.studentId   = data.studentId ?: 0L
        tokenStore.studentName = data.fullName
        tokenStore.admissionNo = data.admissionNo
        tokenStore.className   = data.className
    }
}
