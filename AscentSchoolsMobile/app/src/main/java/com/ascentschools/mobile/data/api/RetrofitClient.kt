package com.ascentschools.mobile.data.api

import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.data.local.TokenStore
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private const val BASE_URL = "https://edu-care.in/api/"
    //private const val BASE_URL = "http://192.168.0.102:62845/"

    private var _tokenStore: TokenStore? = null

    fun init(tokenStore: TokenStore) {
        _tokenStore = tokenStore
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.NONE
    }

    private val okHttpClient: OkHttpClient by lazy {
        val store = requireNotNull(_tokenStore) { "Call RetrofitClient.init(tokenStore) first" }
        OkHttpClient.Builder()
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
