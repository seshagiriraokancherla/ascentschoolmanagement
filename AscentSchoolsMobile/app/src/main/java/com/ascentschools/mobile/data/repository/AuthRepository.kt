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
            tokenStore.brandingPrimaryColor = body.data.primaryColor
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
            body.data.refreshToken?.let { tokenStore.refreshToken = it }
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
            body.data.refreshToken?.let { tokenStore.refreshToken = it }
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
            body.data.refreshToken?.let { tokenStore.refreshToken = it }
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
            val stored = tokenStore.refreshToken ?: ""
            val resp = api.refreshParentToken(stored)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Session expired")
            tokenStore.accessToken = body.data.accessToken
            // Server rotates the refresh token on every refresh — save the new one.
            body.data.refreshToken?.let { tokenStore.refreshToken = it }
            tokenStore.tokenType   = body.data.tokenType
            body.data.fullName?.let { tokenStore.studentName = it }
        }
    }

    suspend fun silentRefreshTeacher(): Result<Unit> {
        return runCatching {
            val stored = tokenStore.refreshToken ?: ""
            val resp = api.refreshTeacherToken(stored)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Session expired")
            tokenStore.accessToken = body.data.accessToken
            body.data.refreshToken?.let { tokenStore.refreshToken = it }
            body.data.fullName?.let { tokenStore.studentName = it }
        }
    }

    /**
     * Opportunistic background refresh, run once after the UI is already up.
     *
     * Its ONLY job is to slide the server's 365-day refresh window forward and renew the
     * access token for parents who open the app regularly. It is not on the critical path:
     * the app is already usable when this runs, and it NEVER clears the session — on any
     * failure the previous token is left exactly as it was. (The cold-start refresh that
     * used to gate the UI and wipe the session on failure is gone; that call was the single
     * biggest cause of parents being sent back to the OTP screen.)
     *
     * The refresh endpoint returns a parent token WITHOUT child context, so the last child
     * is re-selected immediately after. If that second step fails, the previous
     * child-context token is restored — a child-less token would leave every data screen
     * failing with "Please select a child first".
     *
     * @return true if the session was renewed; false if it was left untouched.
     */
    suspend fun refreshSessionQuietly(): Boolean {
        val previousToken = tokenStore.accessToken ?: return false
        val isTeacher     = tokenStore.userType == "teacher"

        // On failure silentRefresh* leaves TokenStore untouched (it only writes after a
        // successful parse), so there is nothing to roll back here.
        if ((if (isTeacher) silentRefreshTeacher() else silentRefresh()).isFailure) return false

        if (!isTeacher) {
            val linkId = tokenStore.childLinkId
            if (linkId > 0 && selectChild(linkId).isFailure) {
                tokenStore.accessToken = previousToken
                return false
            }
        }
        return true
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
