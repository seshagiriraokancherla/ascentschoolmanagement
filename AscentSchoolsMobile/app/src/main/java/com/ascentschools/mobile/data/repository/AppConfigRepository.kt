package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.api.ApiService

/** Result of the launch-time version check. */
sealed class UpdateStatus {
    /** App is up to date (or the check failed — we fail open). Proceed normally. */
    object Ok : UpdateStatus()
    /** A newer version exists but the current one still works — optional nudge. */
    data class Soft(val message: String?, val storeUrl: String?) : UpdateStatus()
    /** Current version is below the server-mandated minimum — hard block. */
    data class Forced(val message: String?, val storeUrl: String?) : UpdateStatus()
}

/**
 * Checks the installed versionCode against the server's app_config gate.
 * ALWAYS fails open: any network/parse/server error returns [UpdateStatus.Ok]
 * so a backend hiccup can never lock users out of the app.
 */
class AppConfigRepository(private val api: ApiService) {

    suspend fun checkVersion(): UpdateStatus {
        return runCatching {
            val resp = api.getAppConfig(
                applicationId = BuildConfig.APPLICATION_ID,
                platform      = "android",
                versionCode   = BuildConfig.VERSION_CODE
            )
            val body = resp.body()
            if (!resp.isSuccessful || body == null || !body.success || body.data == null)
                return UpdateStatus.Ok   // fail open

            val d = body.data
            when {
                d.updateRequired  -> UpdateStatus.Forced(d.message, d.storeUrl)
                d.updateAvailable -> UpdateStatus.Soft(d.message, d.storeUrl)
                else              -> UpdateStatus.Ok
            }
        }.getOrDefault(UpdateStatus.Ok)   // fail open on any exception
    }
}
