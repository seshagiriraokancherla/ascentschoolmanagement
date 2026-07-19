package com.ascentschools.mobile

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import com.ascentschools.mobile.data.api.RetrofitClient
import com.ascentschools.mobile.data.local.TokenStore

class AscentApp : Application() {

    lateinit var tokenStore: TokenStore
        private set

    override fun onCreate() {
        super.onCreate()
        tokenStore = TokenStore(this)
        RetrofitClient.init(tokenStore, applicationContext)
        createNotificationChannel()
    }

    // Channel used by FCM background messages (referenced in the manifest meta-data)
    // and foreground notifications built by AscentFirebaseMessagingService.
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                "ascent_default",
                "School Updates",
                NotificationManager.IMPORTANCE_HIGH
            ).apply { description = "Announcements, homework and events from your school" }
            getSystemService(NotificationManager::class.java)?.createNotificationChannel(channel)
        }
    }
}
