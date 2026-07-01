package com.ascentschools.mobile.data.local

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import java.util.UUID

class TokenStore(context: Context) {

    private val prefs: SharedPreferences = try {
        EncryptedSharedPreferences.create(
            context,
            "ascent_secure_prefs",
            MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
    } catch (e: Exception) {
        // EncryptedSharedPreferences failed (corrupted keystore or Tink issue).
        // Fall back to plain prefs — user will need to log in again.
        Log.e("TokenStore", "EncryptedSharedPreferences init failed, falling back", e)
        context.getSharedPreferences("ascent_fallback_prefs", Context.MODE_PRIVATE)
    }

    var accessToken: String?
        get()      = prefs.getString(KEY_ACCESS_TOKEN, null)
        set(value) = prefs.edit().putString(KEY_ACCESS_TOKEN, value).apply()

    // Refresh token is stored as an HttpOnly cookie by the server.
    // We store only the session metadata here.

    var tokenType: String?
        get()      = prefs.getString(KEY_TOKEN_TYPE, null)
        set(value) = prefs.edit().putString(KEY_TOKEN_TYPE, value).apply()

    /** "parent" or "teacher" — determines which home screen to show after login. */
    var userType: String?
        get()      = prefs.getString(KEY_USER_TYPE, null)
        set(value) = prefs.edit().putString(KEY_USER_TYPE, value).apply()

    var studentName: String?
        get()      = prefs.getString(KEY_STUDENT_NAME, null)
        set(value) = prefs.edit().putString(KEY_STUDENT_NAME, value).apply()

    var studentId: Long
        get()      = prefs.getLong(KEY_STUDENT_ID, 0L)
        set(value) = prefs.edit().putLong(KEY_STUDENT_ID, value).apply()

    /** parent_children.link_id of the selected child — used to re-establish child
     *  context after a token refresh (refresh returns a parent token without it). */
    var childLinkId: Int
        get()      = prefs.getInt(KEY_CHILD_LINK_ID, 0)
        set(value) = prefs.edit().putInt(KEY_CHILD_LINK_ID, value).apply()

    var admissionNo: String?
        get()      = prefs.getString(KEY_ADMISSION_NO, null)
        set(value) = prefs.edit().putString(KEY_ADMISSION_NO, value).apply()

    var className: String?
        get()      = prefs.getString(KEY_CLASS_NAME, null)
        set(value) = prefs.edit().putString(KEY_CLASS_NAME, value).apply()

    /**
     * Runtime-selected school code (subdomain, used as X-School-Code) for the generic
     * single-app build. Empty for baked per-school flavors. Survives logout (one-time
     * selection); cleared only via clearSchool() ("Change School").
     */
    var schoolCode: String?
        get()      = prefs.getString(KEY_SCHOOL_CODE, null)
        set(value) = prefs.edit().putString(KEY_SCHOOL_CODE, value).apply()

    /** Cached branding for the selected school so the login screen isn't blank on cold start. */
    var brandingName: String?
        get()      = prefs.getString(KEY_BRANDING_NAME, null)
        set(value) = prefs.edit().putString(KEY_BRANDING_NAME, value).apply()

    var brandingLogoUrl: String?
        get()      = prefs.getString(KEY_BRANDING_LOGO, null)
        set(value) = prefs.edit().putString(KEY_BRANDING_LOGO, value).apply()

    /** Stable device identifier — auto-generated on first launch, never cleared on logout. */
    val deviceId: String
        get() {
            var id = prefs.getString(KEY_DEVICE_ID, null)
            if (id == null) {
                id = UUID.randomUUID().toString()
                prefs.edit().putString(KEY_DEVICE_ID, id).apply()
            }
            return id
        }

    val isLoggedIn: Boolean
        get() = accessToken != null

    /** Logout — preserves deviceId AND the selected school + branding (one-time school pick). */
    fun clear() {
        val savedDeviceId = deviceId
        val savedSchool   = schoolCode
        val savedName     = brandingName
        val savedLogo     = brandingLogoUrl
        prefs.edit().clear().apply()
        prefs.edit()
            .putString(KEY_DEVICE_ID, savedDeviceId)
            .putString(KEY_SCHOOL_CODE, savedSchool)
            .putString(KEY_BRANDING_NAME, savedName)
            .putString(KEY_BRANDING_LOGO, savedLogo)
            .apply()
    }

    /** Change School — clears the selected school + cached branding (keeps deviceId). */
    fun clearSchool() {
        prefs.edit()
            .remove(KEY_SCHOOL_CODE)
            .remove(KEY_BRANDING_NAME)
            .remove(KEY_BRANDING_LOGO)
            .apply()
    }

    companion object {
        private const val KEY_ACCESS_TOKEN  = "access_token"
        private const val KEY_TOKEN_TYPE    = "token_type"
        private const val KEY_USER_TYPE     = "user_type"
        private const val KEY_STUDENT_NAME  = "student_name"
        private const val KEY_STUDENT_ID    = "student_id"
        private const val KEY_CHILD_LINK_ID = "child_link_id"
        private const val KEY_ADMISSION_NO  = "admission_no"
        private const val KEY_CLASS_NAME    = "class_name"
        private const val KEY_DEVICE_ID     = "device_id"
        private const val KEY_SCHOOL_CODE   = "school_code"
        private const val KEY_BRANDING_NAME = "branding_name"
        private const val KEY_BRANDING_LOGO = "branding_logo"
    }
}
