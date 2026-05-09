package com.ascentschools.mobile

import android.app.Application
import com.ascentschools.mobile.data.api.RetrofitClient
import com.ascentschools.mobile.data.local.TokenStore

class AscentApp : Application() {

    lateinit var tokenStore: TokenStore
        private set

    override fun onCreate() {
        super.onCreate()
        tokenStore = TokenStore(this)
        RetrofitClient.init(tokenStore)
    }
}
