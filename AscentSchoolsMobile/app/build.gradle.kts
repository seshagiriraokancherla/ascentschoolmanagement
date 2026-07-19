import java.io.FileInputStream
import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    id("com.google.gms.google-services")
}

// Release signing credentials live in keystore.properties at the project root
// (gitignored). If the file is absent (e.g. a fresh clone), release builds fall
// back to unsigned and the Generate Signed Bundle/APK wizard still works.
val keystorePropertiesFile = rootProject.file("keystore.properties")
val keystoreProperties = Properties().apply {
    if (keystorePropertiesFile.exists()) load(FileInputStream(keystorePropertiesFile))
}

android {
    namespace  = "com.ascentschools.mobile"
    compileSdk = 35

    defaultConfig {
        applicationId = "in.educare.mobile"   // overridden by each flavor
        minSdk        = 24
        targetSdk     = 35
        versionCode   = 29
        versionName   = "2.9"
    }

    // ── White-label school flavors ────────────────────────────────────────────
    // Each school gets its own applicationId, app name, and SCHOOL_CODE.
    // Build a specific school APK:  ./gradlew assembleSrividyaRelease
    // In Android Studio: select the flavor from Build Variants panel.
    //
    // To add a new school, copy one flavor block and update all three fields.
    // Drop the school-specific launcher icon in:
    //   src/{flavorName}/res/mipmap-*/ic_launcher.png
    //   src/{flavorName}/res/mipmap-*/ic_launcher_round.png

    flavorDimensions += "school"

    productFlavors {
        create("stannsasf") {
            dimension     = "school"
            applicationId = "in.educare.stannsasf"
            buildConfigField("String", "SCHOOL_CODE", "\"stannsasf\"")
            resValue("string", "app_name", "St Anns Girls School Asif Nagar")
        }
        // ── Template for a new school ─────────────────────────────────────────
         create("holyspiritjm") {
             dimension     = "school"
             applicationId = "in.educare.holyspiritjm"
              buildConfigField("String", "SCHOOL_CODE", "\"holyspiritjm\"")
              resValue("string", "app_name", "Holy Spirit School Jeedimetla")
          }

        create("Demo") {
            dimension     = "school"
            applicationId = "in.educare.demo"
            buildConfigField("String", "SCHOOL_CODE", "\"demo\"")
            resValue("string", "app_name", "Demo School Hyderabad")
        }


        create("depaulemyv") {
            dimension     = "school"
            applicationId = "in.educare.depaulemyv"
            buildConfigField("String", "SCHOOL_CODE", "\"depaulemyv\"")
            resValue("string", "app_name", "DePaul EM School")
        }

        // ── Generic single (multi-school) app — used by ALL new schools ───────────
        // SCHOOL_CODE is empty → the app asks the parent for a 4-digit school code at
        // first launch (resolved at runtime). Generic launcher name + icon.
        create("ascent") {
            dimension     = "school"
            applicationId = "in.educare.app"
            buildConfigField("String", "SCHOOL_CODE", "\"\"")
            resValue("string", "app_name", "Ascent Schools")
        }
    }

    signingConfigs {
        create("release") {
            if (keystorePropertiesFile.exists()) {
                storeFile     = file(keystoreProperties["storeFile"] as String)
                storePassword = keystoreProperties["storePassword"] as String
                keyAlias      = keystoreProperties["keyAlias"] as String
                keyPassword   = keystoreProperties["keyPassword"] as String
            }
        }
    }

    buildTypes {
        debug {
            // Local dev API (IIS Express binds to localhost only).
            // Use `localhost` + run once per adb session:  adb reverse tcp:62845 tcp:62845
            //   → device/emulator localhost:62845 forwards to the host's IIS Express.
            // (10.0.2.2 fails because IIS Express refuses the forwarded non-localhost connection.)
            // Physical device alternative: USB + the same `adb reverse` command works too.
            // Trailing slash required by Retrofit; no "/api" suffix locally (prod proxy adds it).
            buildConfigField("String", "API_BASE_URL", "\"http://localhost:62845/\"")
        }
        release {
            if (keystorePropertiesFile.exists()) {
                signingConfig = signingConfigs.getByName("release")
            }
            isMinifyEnabled = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            buildConfigField("String", "API_BASE_URL", "\"https://edu-care.in/api/\"")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        compose      = true
        buildConfig  = true
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime)
    implementation(libs.androidx.lifecycle.viewmodel)
    implementation(libs.androidx.activity.compose)

    implementation(platform(libs.compose.bom))
    implementation(libs.compose.ui)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.compose.material3)
    implementation(libs.compose.material.icons)
    debugImplementation(libs.compose.ui.tooling)

    implementation(libs.navigation.compose)

    implementation(libs.retrofit)
    implementation(libs.retrofit.gson)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)

    implementation(libs.security.crypto)
    implementation(libs.coroutines.android)
    implementation(libs.gson)

    // Razorpay checkout SDK
    implementation("com.razorpay:checkout:1.6.40")

    // Coil — image loading for Compose (AsyncImage)
    implementation("io.coil-kt:coil-compose:2.6.0")

    // Google Play In-App Updates — replaces the old app_config versionCode gate.
    // Asks Play directly whether a newer build is available to this device.
    implementation("com.google.android.play:app-update:2.1.0")
    implementation("com.google.android.play:app-update-ktx:2.1.0")

    // Firebase Cloud Messaging — push notifications (Feature 3).
    implementation(platform("com.google.firebase:firebase-bom:33.5.1"))
    implementation("com.google.firebase:firebase-messaging")
}
