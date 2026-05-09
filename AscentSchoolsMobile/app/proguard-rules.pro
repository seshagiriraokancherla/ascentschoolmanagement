# Retrofit — keep generic signatures for type-safe calls
-keepattributes Signature
-keepattributes *Annotation*
-dontwarn retrofit2.**
-keep class retrofit2.** { *; }

# Gson — keep data classes used in API responses
-keep class com.ascentschools.mobile.data.api.** { *; }

# OkHttp
-dontwarn okhttp3.**
-dontwarn okio.**

# Kotlin coroutines
-keepnames class kotlinx.coroutines.internal.MainDispatcherFactory {}
-keepnames class kotlinx.coroutines.CoroutineExceptionHandler {}

# Keep ViewModel subclasses
-keep class * extends androidx.lifecycle.ViewModel { *; }

# EncryptedSharedPreferences / Security Crypto
-keep class androidx.security.crypto.** { *; }

# Google Tink — used internally by EncryptedSharedPreferences
# R8 strips these in release builds, causing a crash on app startup
-keep class com.google.crypto.tink.** { *; }
-dontwarn com.google.crypto.tink.**
-keepclassmembers class com.google.crypto.tink.** { *; }

# Tink uses reflection to load key type managers
-keepnames class * implements com.google.crypto.tink.KeyTypeManager
-keepnames class * implements com.google.crypto.tink.PrivateKeyTypeManager

# Razorpay
-keepclassmembers class com.razorpay.** { *; }
-keep class com.razorpay.** { *; }
-dontwarn com.razorpay.**
-keep class org.json.** { *; }
