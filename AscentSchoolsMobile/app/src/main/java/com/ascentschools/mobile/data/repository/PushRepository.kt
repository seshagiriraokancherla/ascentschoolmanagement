package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.api.ApiService
import com.ascentschools.mobile.data.api.RegisterPushTokenRequest
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume

/**
 * Registers/unregisters this device's FCM token with the backend.
 * Call registerToken() after every login and after a cold-start silent refresh
 * (the token is stable but the server row must exist for the current user), and
 * unregisterToken() on logout.
 */
class PushRepository(private val api: ApiService) {

    suspend fun registerToken(): Result<Unit> = runCatching {
        val fcm = fetchFcmToken() ?: error("No FCM token")
        val resp = api.registerPushToken(
            RegisterPushTokenRequest(fcm, BuildConfig.APPLICATION_ID, "android")
        )
        if (!resp.isSuccessful) error("register failed: HTTP ${resp.code()}")
    }

    suspend fun unregisterToken(): Result<Unit> = runCatching {
        val fcm = fetchFcmToken() ?: return@runCatching
        // Best-effort — ignore the response; the row is also re-bound on next login.
        api.unregisterPushToken(RegisterPushTokenRequest(fcm, BuildConfig.APPLICATION_ID, "android"))
    }

    private suspend fun fetchFcmToken(): String? = suspendCancellableCoroutine { cont ->
        FirebaseMessaging.getInstance().token
            .addOnSuccessListener { token -> cont.resume(token) }
            .addOnFailureListener { cont.resume(null) }
    }
}
