package com.ascentschools.mobile.data.api

import android.content.Context
import android.content.SharedPreferences
import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.local.TokenStore
import com.google.gson.Gson
import com.google.gson.JsonObject
import com.google.gson.reflect.TypeToken
import okhttp3.Authenticator
import okhttp3.Cookie
import okhttp3.CookieJar
import okhttp3.HttpUrl
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import kotlinx.coroutines.flow.MutableStateFlow
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

// ── Persistent cookie jar ─────────────────────────────────────────────────────
// Serializes OkHttp cookies to SharedPreferences so the HttpOnly refresh token
// cookie survives app kill/restart. Without this, every cold start requires OTP.

private data class SerializedCookie(
    val name: String,
    val value: String,
    val domain: String,
    val path: String,
    val expiresAt: Long,
    val secure: Boolean,
    val httpOnly: Boolean
)

class PersistentCookieJar(prefs: SharedPreferences) : CookieJar {

    private val prefs = prefs
    private val gson  = Gson()
    private val lock  = Any()
    private val cache = mutableMapOf<String, Cookie>()

    init { loadFromPrefs() }

    override fun saveFromResponse(url: HttpUrl, cookies: List<Cookie>) {
        synchronized(lock) {
            cookies.forEach { cookie -> cache["${cookie.domain}|${cookie.name}"] = cookie }
            persistToPrefs()
        }
    }

    override fun loadForRequest(url: HttpUrl): List<Cookie> {
        synchronized(lock) {
            return cache.values.filter { it.matches(url) }
        }
    }

    private fun persistToPrefs() {
        val list = cache.values.map { c ->
            SerializedCookie(c.name, c.value, c.domain, c.path,
                             c.expiresAt, c.secure, c.httpOnly)
        }
        prefs.edit().putString(PREF_KEY, gson.toJson(list)).apply()
    }

    private fun loadFromPrefs() {
        val json = prefs.getString(PREF_KEY, null) ?: return
        runCatching {
            val type = object : TypeToken<List<SerializedCookie>>() {}.type
            val list: List<SerializedCookie> = gson.fromJson(json, type) ?: return
            list.forEach { sc ->
                val cookie = Cookie.Builder()
                    .name(sc.name)
                    .value(sc.value)
                    .domain(sc.domain)
                    .path(sc.path)
                    .expiresAt(sc.expiresAt)
                    .apply { if (sc.secure)   secure() }
                    .apply { if (sc.httpOnly) httpOnly() }
                    .build()
                cache["${sc.domain}|${sc.name}"] = cookie
            }
        }
    }

    companion object {
        private const val PREF_KEY = "persistent_cookies"
    }
}

// ─────────────────────────────────────────────────────────────────────────────

object RetrofitClient {

    // Per-build-type: debug → local dev API, release → production (see app/build.gradle.kts).
    private val BASE_URL = BuildConfig.API_BASE_URL

    /** Base for resolving relative media paths (branding logos, /Uploads/...).
     *  The Uploads folder is served UNDER the API app (e.g. edu-care.in/api/Uploads/...),
     *  NOT the site root — so keep the full BASE_URL including the trailing "api/".
     *  Stripping "api/" here made every branding logo URL 404. */
    val mediaBaseUrl: String = BASE_URL

    private var _tokenStore: TokenStore? = null
    private var _cookieJar: PersistentCookieJar? = null

    /**
     * Raised only when the session is provably dead: a real API call returned 401 AND the
     * subsequent refresh was explicitly rejected by the server (401/403) — i.e. the refresh
     * token was revoked or is 365 days idle. Two independent rejections, so this cannot be
     * tripped by a flaky network.
     *
     * This is the ONLY automatic logout in the app. It replaces the old cold-start
     * "refresh failed for any reason → wipe the session" behaviour, which treated an
     * unreachable server exactly like a revoked token and logged people out daily.
     */
    val sessionExpired = MutableStateFlow(false)

    fun init(tokenStore: TokenStore, context: Context) {
        _tokenStore = tokenStore
        _cookieJar  = PersistentCookieJar(
            context.getSharedPreferences("ascent_cookies", Context.MODE_PRIVATE)
        )
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.NONE
    }

    // ── In-session token refresh (401 -> refresh -> retry) ────────────────────
    // Keeps the app logged in for the full 7-day sliding refresh window: when the
    // access token expires mid-session, OkHttp calls this Authenticator on the 401,
    // it silently refreshes using the persisted cookie and retries the request.
    private val refreshLock = Any()

    // Separate client with NO authenticator (avoids recursion) but the SAME cookie jar
    // so the HttpOnly refresh-token cookie is sent.
    private val refreshClient: OkHttpClient by lazy {
        OkHttpClient.Builder()
            .cookieJar(requireNotNull(_cookieJar))
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(60, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .build()
    }

    private fun responseCount(response: Response): Int {
        var r: Response? = response.priorResponse
        var n = 1
        while (r != null) { n++; r = r.priorResponse }
        return n
    }

    private fun applySchoolHeaders(b: Request.Builder, store: TokenStore) {
        val code = BuildConfig.SCHOOL_CODE.ifBlank { store.schoolCode ?: "" }
        if (code.isNotBlank()) { b.header("X-School-Code", code); b.header("X-Subdomain", code) }
    }

    private fun dataField(bodyStr: String?, field: String): String? =
        bodyStr?.let { Gson().fromJson(it, JsonObject::class.java).getAsJsonObject("data")?.get(field)?.asString }

    private fun parseAccessToken(bodyStr: String?): String? = dataField(bodyStr, "accessToken")

    /** Calls the parent/teacher refresh endpoint, sending the app-held refresh token in
     *  the X-Refresh-Token header (the HttpOnly cookie is unreliable across app kill).
     *  Returns the new access token, or null if refresh failed. The server rotates the
     *  refresh token — the new value is saved back to TokenStore. For a parent, also
     *  re-selects the last child so child-context endpoints keep working (refresh alone
     *  returns a parent token with no child context). */
    private fun refreshAccessToken(store: TokenStore): String? {
        val endpoint = if (store.userType == "teacher") "mobile/auth/teacher/refresh"
                       else "mobile/auth/parent/refresh"
        return runCatching {
            val refreshReq = Request.Builder().url(BASE_URL + endpoint)
                .post(ByteArray(0).toRequestBody(null))
                .header("X-Refresh-Token", store.refreshToken ?: "")
                .also { applySchoolHeaders(it, store) }.build()
            var token = refreshClient.newCall(refreshReq).execute().use { resp ->
                if (!resp.isSuccessful) {
                    // Explicit rejection = the refresh token really is dead. Any other
                    // failure (timeout, DNS, 5xx) is transient and must NOT end the session.
                    if (resp.code == 401 || resp.code == 403) sessionExpired.value = true
                    return null
                }
                val bodyStr = resp.body?.string()
                // Persist the rotated refresh token so the next refresh doesn't reuse a
                // revoked one.
                dataField(bodyStr, "refreshToken")?.let { store.refreshToken = it }
                parseAccessToken(bodyStr)
            } ?: return null

            // Parent: re-establish child context with the refreshed token.
            if (store.userType != "teacher" && store.childLinkId > 0) {
                runCatching {
                    val selReq = Request.Builder().url(BASE_URL + "mobile/auth/parent/select-child")
                        .post(("{\"linkId\":" + store.childLinkId + "}")
                            .toRequestBody("application/json; charset=utf-8".toMediaTypeOrNull()))
                        .header("Authorization", "Bearer $token")
                        .also { applySchoolHeaders(it, store) }.build()
                    refreshClient.newCall(selReq).execute().use { resp ->
                        if (resp.isSuccessful) parseAccessToken(resp.body?.string())?.let { token = it }
                    }
                }
            }
            token
        }.getOrNull()
    }

    private val tokenAuthenticator = Authenticator { _, response ->
        val store = _tokenStore ?: return@Authenticator null
        // Never refresh the auth calls themselves; cap retries to avoid loops.
        if (response.request.url.encodedPath.contains("/auth/")) return@Authenticator null
        if (responseCount(response) >= 2) return@Authenticator null

        synchronized(refreshLock) {
            val failedToken = response.request.header("Authorization")?.removePrefix("Bearer ")
            val current     = store.accessToken
            // Another request may have already refreshed the token while we waited.
            val token = if (current != null && current != failedToken) current
                        else refreshAccessToken(store)?.also { store.accessToken = it }
            if (token == null) return@Authenticator null
            response.request.newBuilder().header("Authorization", "Bearer $token").build()
        }
    }

    private val okHttpClient: OkHttpClient by lazy {
        val store     = requireNotNull(_tokenStore) { "Call RetrofitClient.init() first" }
        val cookieJar = requireNotNull(_cookieJar)  { "Call RetrofitClient.init() first" }
        OkHttpClient.Builder()
            .cookieJar(cookieJar)
            .authenticator(tokenAuthenticator)
            .addInterceptor { chain ->
                val original = chain.request()
                val builder  = original.newBuilder()
                store.accessToken?.let { builder.header("Authorization", "Bearer $it") }
                // Per-school flavors bake SCHOOL_CODE; the generic single app leaves it empty
                // and resolves the school at runtime (stored in TokenStore.schoolCode).
                // Send both X-School-Code (auth/data) and X-Subdomain (branding endpoint).
                val code = BuildConfig.SCHOOL_CODE.ifBlank { store.schoolCode ?: "" }
                if (code.isNotBlank()) {
                    builder.header("X-School-Code", code)
                    builder.header("X-Subdomain",   code)
                }
                // Identifies the Android app to the server (e.g. receipt "Collected by").
                // The parent web portal sends no such header and defaults to "Parent Portal".
                builder.header("X-Client-App", "mobile")
                chain.proceed(builder.build())
            }
            .addInterceptor(loggingInterceptor)
            // Longer read timeout: request-otp blocks on the SMS gateway (smslogin.mobi),
            // which is sometimes slow. Must stay > the API's gateway timeout (45s) so the
            // server returns a real result/error before the app gives up.
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(60, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .callTimeout(75, TimeUnit.SECONDS)
            .build()
    }

    val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
