package com.ascentschools.mobile.data.local

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import java.util.UUID

class TokenStore(context: Context) {

    /**
     * Session data lives in app-private plain SharedPreferences.
     *
     * It used to live in EncryptedSharedPreferences, which turned out to be the wrong
     * trade here. androidx.security-crypto is deprecated, and its Keystore-backed keyset
     * is known to fail on some OEM devices and after a backup / device-to-device transfer.
     * When it failed, the old code fell back to a DIFFERENT, EMPTY prefs file — which reads
     * as "never logged in", i.e. a silent logout with no error surfaced anywhere. That is
     * one of the causes of parents being sent back to the OTP screen.
     *
     * App-private storage is not readable by other apps or without root, and is where
     * Firebase Auth keeps its own tokens, so this is the standard trade-off for a bearer
     * token. `allowBackup` is off (see AndroidManifest) so a session is never restored onto
     * a different device — which device binding would reject anyway.
     */
    private val prefs: SharedPreferences =
        context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    init { migrateFromEncryptedPrefs(context) }

    /**
     * One-time import of an existing session from the old encrypted store. Without this,
     * shipping this build would log every user out on update — exactly the bug being fixed.
     *
     * Best-effort: if the encrypted store can't be opened (the very failure that caused the
     * silent logouts), the user logs in once more and never hits it again. The `migrated`
     * flag makes this run exactly once, so an explicit logout can't be undone by a later
     * re-import of the stale session.
     */
    private fun migrateFromEncryptedPrefs(context: Context) {
        if (prefs.getBoolean(KEY_MIGRATED, false)) return
        runCatching {
            val legacy = EncryptedSharedPreferences.create(
                context,
                LEGACY_PREFS_NAME,
                MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
                EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
            )
            val editor = prefs.edit()
            legacy.all.forEach { (key, value) ->
                when (value) {
                    is String  -> editor.putString(key, value)
                    is Int     -> editor.putInt(key, value)
                    is Long    -> editor.putLong(key, value)
                    is Boolean -> editor.putBoolean(key, value)
                    is Float   -> editor.putFloat(key, value)
                }
            }
            editor.apply()
        }.onFailure {
            Log.w("TokenStore", "No legacy session migrated (encrypted store unreadable)", it)
        }

        // Users whose encrypted store had already failed were parked in the old plain
        // fallback file — the people this bug hit hardest. Pick their session up too.
        if (prefs.getString(KEY_ACCESS_TOKEN, null) == null) {
            runCatching {
                val fallback = context.getSharedPreferences(
                    LEGACY_FALLBACK_PREFS_NAME, Context.MODE_PRIVATE
                )
                val editor = prefs.edit()
                fallback.all.forEach { (key, value) ->
                    when (value) {
                        is String  -> editor.putString(key, value)
                        is Int     -> editor.putInt(key, value)
                        is Long    -> editor.putLong(key, value)
                        is Boolean -> editor.putBoolean(key, value)
                        is Float   -> editor.putFloat(key, value)
                    }
                }
                editor.apply()
            }
        }

        prefs.edit().putBoolean(KEY_MIGRATED, true).apply()
    }

    var accessToken: String?
        get()      = prefs.getString(KEY_ACCESS_TOKEN, null)
        set(value) = prefs.edit().putString(KEY_ACCESS_TOKEN, value).apply()

    /**
     * Raw refresh token, held by the app and sent explicitly in the X-Refresh-Token
     * header on refresh. This is the load-bearing session-persistence mechanism —
     * the server's HttpOnly cookie is unreliable across app kill on Android.
     * The server ROTATES this on every refresh, so it must be re-saved from each
     * refresh/login response. Cleared on logout.
     */
    var refreshToken: String?
        get()      = prefs.getString(KEY_REFRESH_TOKEN, null)
        set(value) = prefs.edit().putString(KEY_REFRESH_TOKEN, value).apply()

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

    /** School branding primary color hex (e.g. "#1E3A8A") — tile dashboard band + tint. */
    var brandingPrimaryColor: String?
        get()      = prefs.getString(KEY_BRANDING_COLOR, null)
        set(value) = prefs.edit().putString(KEY_BRANDING_COLOR, value).apply()

    /**
     * Navigation view preference. false = classic bottom tabs (default); true = the
     * premium tile dashboard. A UX choice, so it survives logout (see clear()).
     */
    var tilesView: Boolean
        get()      = prefs.getBoolean(KEY_TILES_VIEW, false)
        set(value) = prefs.edit().putBoolean(KEY_TILES_VIEW, value).apply()

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
        val savedColor    = brandingPrimaryColor
        val savedTiles    = tilesView
        prefs.edit().clear().apply()
        prefs.edit()
            .putString(KEY_DEVICE_ID, savedDeviceId)
            .putString(KEY_SCHOOL_CODE, savedSchool)
            .putString(KEY_BRANDING_NAME, savedName)
            .putString(KEY_BRANDING_LOGO, savedLogo)
            .putString(KEY_BRANDING_COLOR, savedColor)
            .putBoolean(KEY_TILES_VIEW, savedTiles)
            // Must survive: without it the next launch would re-import the old
            // encrypted session and undo this logout.
            .putBoolean(KEY_MIGRATED, true)
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
        private const val PREFS_NAME                 = "ascent_session_prefs"
        private const val LEGACY_PREFS_NAME          = "ascent_secure_prefs"
        private const val LEGACY_FALLBACK_PREFS_NAME = "ascent_fallback_prefs"
        private const val KEY_MIGRATED      = "migrated_from_encrypted"
        private const val KEY_ACCESS_TOKEN  = "access_token"
        private const val KEY_REFRESH_TOKEN = "refresh_token"
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
        private const val KEY_BRANDING_COLOR = "branding_color"
        private const val KEY_TILES_VIEW    = "tiles_view"
    }
}
