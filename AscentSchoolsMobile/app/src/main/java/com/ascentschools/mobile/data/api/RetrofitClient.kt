package com.ascentschools.mobile.data.api

import android.content.Context
import android.content.SharedPreferences
import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.local.TokenStore
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import okhttp3.Cookie
import okhttp3.CookieJar
import okhttp3.HttpUrl
import okhttp3.OkHttpClient
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

    private const val BASE_URL = "https://edu-care.in/api/"
    //private const val BASE_URL = "http://192.168.0.102:62845/"

    private var _tokenStore: TokenStore? = null
    private var _cookieJar: PersistentCookieJar? = null

    fun init(tokenStore: TokenStore, context: Context) {
        _tokenStore = tokenStore
        _cookieJar  = PersistentCookieJar(
            context.getSharedPreferences("ascent_cookies", Context.MODE_PRIVATE)
        )
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.NONE
    }

    private val okHttpClient: OkHttpClient by lazy {
        val store     = requireNotNull(_tokenStore) { "Call RetrofitClient.init() first" }
        val cookieJar = requireNotNull(_cookieJar)  { "Call RetrofitClient.init() first" }
        OkHttpClient.Builder()
            .cookieJar(cookieJar)
            .addInterceptor { chain ->
                val original = chain.request()
                val builder  = original.newBuilder()
                store.accessToken?.let { builder.header("Authorization", "Bearer $it") }
                // School code is baked in per build flavor — never changes at runtime
                builder.header("X-School-Code", BuildConfig.SCHOOL_CODE)
                chain.proceed(builder.build())
            }
            .addInterceptor(loggingInterceptor)
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
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
