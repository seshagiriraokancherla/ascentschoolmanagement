package com.ascentschools.mobile.push

import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import androidx.core.app.NotificationCompat
import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.MainActivity
import com.ascentschools.mobile.R
import com.ascentschools.mobile.data.api.RegisterPushTokenRequest
import com.ascentschools.mobile.data.api.RetrofitClient
import com.ascentschools.mobile.data.local.TokenStore
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class AscentFirebaseMessagingService : FirebaseMessagingService() {

    private val channelId = "ascent_default"

    // Fired when FCM issues a new token (install, data cleared, restore). Re-register
    // it with the backend — but only if a user is logged in (otherwise there's no
    // parent/teacher context to bind it to; login will register it fresh).
    override fun onNewToken(token: String) {
        val tokenStore = TokenStore(applicationContext)
        if (!tokenStore.isLoggedIn) return
        val api = RetrofitClient.apiService
        CoroutineScope(Dispatchers.IO).launch {
            runCatching {
                api.registerPushToken(
                    RegisterPushTokenRequest(token, BuildConfig.APPLICATION_ID, "android")
                )
            }
        }
    }

    // Fired for foreground messages (and data-only messages in any state). Background
    // notification-payload messages are drawn by the system using the manifest's
    // default channel, so we only need to render here.
    override fun onMessageReceived(message: RemoteMessage) {
        val title = message.notification?.title ?: message.data["title"] ?: "School Update"
        val body  = message.notification?.body  ?: message.data["body"]  ?: ""
        showNotification(title, body)
    }

    private fun showNotification(title: String, body: String) {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
        }
        val pending = PendingIntent.getActivity(
            this, 0, intent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        val notification = NotificationCompat.Builder(this, channelId)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pending)
            .build()

        val manager = getSystemService(NotificationManager::class.java)
        manager?.notify(System.currentTimeMillis().toInt(), notification)
    }
}
