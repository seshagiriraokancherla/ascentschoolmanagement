package com.ascentschools.mobile

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.annotation.RequiresApi
import androidx.core.content.ContextCompat
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import com.ascentschools.mobile.data.api.MobileOrderResponse
import com.ascentschools.mobile.data.repository.AuthRepository
import com.ascentschools.mobile.data.repository.FeeRepository
import com.ascentschools.mobile.data.repository.PushRepository
import com.ascentschools.mobile.data.repository.StudentRepository
import com.ascentschools.mobile.data.repository.TeacherRepository
import com.ascentschools.mobile.ui.update.InAppUpdater
import com.ascentschools.mobile.ui.auth.SmsAuthScreen
import com.ascentschools.mobile.ui.auth.SmsAuthViewModel
import com.ascentschools.mobile.ui.fee.FeeViewModel
import com.ascentschools.mobile.ui.fee.PaymentState
import com.ascentschools.mobile.ui.home.HomeScreen
import com.ascentschools.mobile.ui.school.SchoolCodeScreen
import com.ascentschools.mobile.ui.school.SchoolCodeViewModel
import com.ascentschools.mobile.ui.teacher.TeacherAnnouncementScreen
import com.ascentschools.mobile.ui.teacher.TeacherAttendanceScreen
import com.ascentschools.mobile.ui.teacher.TeacherChatScreen
import com.ascentschools.mobile.ui.teacher.TeacherHomeScreen
import com.ascentschools.mobile.ui.teacher.TeacherHomeworkScreen
import com.ascentschools.mobile.ui.teacher.TeacherMessagesScreen
import com.ascentschools.mobile.ui.teacher.TeacherViewModel
import com.ascentschools.mobile.ui.theme.AscentTheme
import com.razorpay.Checkout
import com.razorpay.PaymentResultWithDataListener
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.json.JSONObject

private sealed class TeacherScreen {
    object Home : TeacherScreen()
    object Messages : TeacherScreen()
    data class Attendance(val classId: Int, val sectionId: Int) : TeacherScreen()
    data class Homework(val classId: Int) : TeacherScreen()
    data class Announcement(val classId: Int) : TeacherScreen()
    data class Chat(val threadId: Int) : TeacherScreen()
}

class MainActivity : ComponentActivity(), PaymentResultWithDataListener {

    // Lazily-created ViewModel — set once user is logged in
    private var feeViewModel: FeeViewModel? = null

    // Play In-App Updates (replaces the old server versionCode gate).
    private lateinit var updater: InAppUpdater

    // Android 13+ runtime notification permission (registered before STARTED).
    private val requestNotifPermission =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { }

    private fun requestNotifPermissionIfNeeded() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS)
                != PackageManager.PERMISSION_GRANTED) {
            requestNotifPermission.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }

    // Register this device's FCM token for the just-logged-in user (best-effort).
    private fun registerPush(pushRepo: PushRepository) {
        CoroutineScope(Dispatchers.IO).launch { runCatching { pushRepo.registerToken() } }
    }

    @RequiresApi(Build.VERSION_CODES.O)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        // Pre-load Razorpay resources in background for faster first-open
        Checkout.preload(applicationContext)

        // Ask Google Play (not our server) whether a newer build is available to
        // this device. Registered before the activity is STARTED (constructor
        // registers an ActivityResult launcher), then checked immediately.
        updater = InAppUpdater(this)
        updater.checkForUpdate()

        val app         = application as AscentApp
        val tokenStore  = app.tokenStore
        val api         = com.ascentschools.mobile.data.api.RetrofitClient.apiService
        val authRepo    = AuthRepository(api, tokenStore)
        val studentRepo = StudentRepository(api)
        val feeRepo     = FeeRepository(api)
        val teacherRepo = TeacherRepository(api)
        val pushRepo    = PushRepository(api)

        setContent {
            AscentTheme {
                var isLoggedIn        by remember { mutableStateOf(tokenStore.isLoggedIn) }
                var userType          by remember { mutableStateOf(tokenStore.userType) }
                // Show a loading indicator while we silently refresh an existing session.
                // Without this, a cold start with an expired access token would immediately
                // show the home screen, then fail on every API call until the user notices.
                var isCheckingSession by remember { mutableStateOf(tokenStore.isLoggedIn) }

                // Generic single app (SCHOOL_CODE empty): the parent must pick a school
                // (4-digit code) before anything else. Baked flavors skip this entirely.
                val isGenericApp = BuildConfig.SCHOOL_CODE.isBlank()
                var needsSchool by remember {
                    mutableStateOf(isGenericApp && tokenStore.schoolCode.isNullOrBlank())
                }

                // App update is handled by Play In-App Updates (see `updater` above),
                // which overlays its own UI outside Compose — no gate state needed here.
                // A FLEXIBLE update that finished downloading surfaces a restart prompt below.

                // On cold start: silently refresh the stored session so the access token
                // is valid before any screen tries to make API calls.
                // The refresh cookie is now persisted by PersistentCookieJar, so this
                // works even after the app was killed and restarted.
                LaunchedEffect(Unit) {
                    if (isCheckingSession) {
                        when (tokenStore.userType) {
                            "parent"  -> authRepo.silentRefresh().onSuccess {
                                // Refresh returns a parent token WITHOUT child context;
                                // re-select the last child so data screens work after restart.
                                val linkId = tokenStore.childLinkId
                                if (linkId > 0) authRepo.selectChild(linkId)
                            }
                            "teacher" -> authRepo.silentRefreshTeacher()
                            else      -> Result.failure<Unit>(Exception("Unknown user type"))
                        }.onFailure {
                            // Refresh failed — session is truly expired; force re-login
                            tokenStore.clear()
                            isLoggedIn = false
                            userType   = ""
                        }
                        isCheckingSession = false
                        // Session restored — ensure the FCM token is registered for this
                        // user (token is stable but the server row must exist) + ask for
                        // notification permission on Android 13+.
                        if (tokenStore.isLoggedIn) {
                            registerPush(pushRepo)
                            requestNotifPermissionIfNeeded()
                        }
                    }
                }

                // A FLEXIBLE update finished downloading in the background → prompt to
                // install. Play's own UI handles IMMEDIATE (blocking) updates.
                if (updater.flexibleDownloaded.value) {
                    AlertDialog(
                        onDismissRequest = { },
                        title   = { Text("Update ready") },
                        text    = { Text("A new version has been downloaded. Restart to install it.") },
                        confirmButton = {
                            TextButton(onClick = { updater.completeUpdate() }) { Text("Restart") }
                        }
                    )
                }

                if (isCheckingSession) {
                    // Still refreshing the stored session before showing any screen.
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                } else if (needsSchool) {
                    val schoolVm = remember { SchoolCodeViewModel(authRepo, tokenStore) }
                    SchoolCodeScreen(viewModel = schoolVm, onResolved = { needsSchool = false })
                } else if (isLoggedIn && userType == "teacher") {
                    val teacherVm = remember { TeacherViewModel(teacherRepo) }
                    var teacherScreen by remember { mutableStateOf<TeacherScreen>(TeacherScreen.Home) }

                    when (val screen = teacherScreen) {
                        is TeacherScreen.Home -> TeacherHomeScreen(
                            teacherName  = tokenStore.studentName ?: "",
                            viewModel    = teacherVm,
                            onAttendance = { classId, sectionId ->
                                teacherScreen = TeacherScreen.Attendance(classId, sectionId)
                            },
                            onHomework   = { classId ->
                                teacherScreen = TeacherScreen.Homework(classId)
                            },
                            onAnnouncements = { classId ->
                                teacherScreen = TeacherScreen.Announcement(classId)
                            },
                            onMessages = { teacherScreen = TeacherScreen.Messages },
                            onLogout = {
                                CoroutineScope(Dispatchers.IO).launch {
                                    runCatching { pushRepo.unregisterToken() }
                                    runCatching { authRepo.logoutTeacher() }
                                }
                                tokenStore.clear()
                                isLoggedIn = false
                                userType   = ""
                            }
                        )
                        is TeacherScreen.Attendance -> TeacherAttendanceScreen(
                            classId   = screen.classId,
                            sectionId = screen.sectionId,
                            viewModel = teacherVm,
                            onBack    = { teacherScreen = TeacherScreen.Home }
                        )
                        is TeacherScreen.Homework -> TeacherHomeworkScreen(
                            classId   = screen.classId,
                            className = teacherVm.classes.value
                                .find { it.classId == screen.classId }?.className ?: "",
                            viewModel = teacherVm,
                            onBack    = { teacherScreen = TeacherScreen.Home }
                        )
                        is TeacherScreen.Announcement -> TeacherAnnouncementScreen(
                            classId   = screen.classId,
                            className = teacherVm.classes.value
                                .find { it.classId == screen.classId }?.className ?: "",
                            viewModel = teacherVm,
                            onBack    = { teacherScreen = TeacherScreen.Home }
                        )
                        is TeacherScreen.Messages -> TeacherMessagesScreen(
                            viewModel = teacherVm,
                            onOpen    = { threadId -> teacherScreen = TeacherScreen.Chat(threadId) },
                            onBack    = { teacherScreen = TeacherScreen.Home }
                        )
                        is TeacherScreen.Chat -> TeacherChatScreen(
                            threadId  = screen.threadId,
                            viewModel = teacherVm,
                            onBack    = { teacherScreen = TeacherScreen.Messages }
                        )
                    }

                } else if (isLoggedIn) {
                    // Parent home. childEpoch is bumped on a child switch so the whole
                    // subtree (feeVm + all tab ViewModels) is recreated and reloads data
                    // for the newly-selected child. The token is already updated by then.
                    var childEpoch by remember { mutableIntStateOf(0) }

                    key(childEpoch) {
                        val feeVm = remember {
                            FeeViewModel(feeRepo).also { feeViewModel = it }
                        }

                        HomeScreen(
                            repo       = studentRepo,
                            feeVm      = feeVm,
                            tokenStore = tokenStore,
                            authRepo   = authRepo,
                            onInitiatePayment = { items, academicYearId, feeTypeCategory ->
                                feeVm.initiatePayment(items, academicYearId, feeTypeCategory)
                            },
                            onChildSwitched = { childEpoch++ },
                            onChangeSchool = if (isGenericApp) {
                                {
                                    CoroutineScope(Dispatchers.IO).launch {
                                        runCatching { pushRepo.unregisterToken() }
                                        runCatching { authRepo.logoutParent() }
                                    }
                                    tokenStore.clear()        // end session
                                    tokenStore.clearSchool()  // drop school + branding
                                    feeViewModel = null
                                    isLoggedIn = false
                                    userType   = ""
                                    needsSchool = true        // back to SchoolCodeScreen
                                }
                            } else null,
                            onLogout = {
                                CoroutineScope(Dispatchers.IO).launch {
                                    runCatching { pushRepo.unregisterToken() }
                                    runCatching { authRepo.logoutParent() }
                                }
                                tokenStore.clear()
                                feeViewModel = null
                                isLoggedIn = false
                                userType   = ""
                            }
                        )

                        // Watch for OrderReady state → open Razorpay checkout
                        LaunchedEffect(feeVm) {
                            feeVm.paymentState.collect { state ->
                                if (state is PaymentState.OrderReady) {
                                    openRazorpayCheckout(state.order)
                                }
                            }
                        }
                    }

                } else {
                    val authVm = remember { SmsAuthViewModel(authRepo) }
                    SmsAuthScreen(
                        viewModel        = authVm,
                        onParentSuccess  = {
                            isLoggedIn = true; userType = tokenStore.userType
                            registerPush(pushRepo); requestNotifPermissionIfNeeded()
                        },
                        onTeacherSuccess = {
                            isLoggedIn = true; userType = "teacher"
                            registerPush(pushRepo); requestNotifPermissionIfNeeded()
                        },
                        onChangeSchool   = if (isGenericApp) {
                            { tokenStore.clearSchool(); needsSchool = true }
                        } else null
                    )
                }
            }
        }
    }

    // ── In-app update lifecycle ───────────────────────────────────────────────

    override fun onResume() {
        super.onResume()
        // Resume an interrupted IMMEDIATE update and surface a FLEXIBLE one that
        // finished downloading while the app was backgrounded.
        if (::updater.isInitialized) updater.onResume()
    }

    override fun onDestroy() {
        if (::updater.isInitialized) updater.destroy()
        super.onDestroy()
    }

    // ── Razorpay checkout ─────────────────────────────────────────────────────

    private fun openRazorpayCheckout(order: MobileOrderResponse) {
        val checkout = Checkout()
        checkout.setKeyID(order.keyId)

        val options = JSONObject().apply {
            put("name",        getString(R.string.app_name))
            put("description", "School Fee Payment")
            put("order_id",    order.externalOrderId)
            put("amount",      order.amountInPaise)
            put("currency",    order.currency)
            put("prefill", JSONObject().apply {
                put("contact", "")
                put("email",   "")
            })
            put("theme", JSONObject().apply {
                put("color", "#1677FF")
            })
        }

        try {
            checkout.open(this, options)
        } catch (e: Exception) {
            Log.e("Razorpay", "Error opening checkout", e)
            feeViewModel?.onPaymentFailed("Could not open payment screen: ${e.message}")
        }
    }

    // ── PaymentResultWithDataListener ─────────────────────────────────────────

    override fun onPaymentSuccess(razorpayPaymentId: String?, paymentData: com.razorpay.PaymentData?) {
        val vm = feeViewModel ?: return
        if (razorpayPaymentId == null) {
            vm.onPaymentFailed("Payment ID missing from response.")
            return
        }
        val orderId   = paymentData?.orderId ?: ""
        val signature = paymentData?.signature ?: ""

        // gatewayOrderId is stored in the ViewModel via OrderReady state
        val currentState = vm.paymentState.value
        val gatewayOrderId = if (currentState is PaymentState.OrderReady)
            currentState.order.gatewayOrderId else 0

        vm.verifyPayment(gatewayOrderId, razorpayPaymentId, orderId, signature)
    }

    override fun onPaymentError(errorCode: Int, errorDescription: String?, paymentData: com.razorpay.PaymentData?) {
        val message = when (errorCode) {
            Checkout.NETWORK_ERROR  -> "Network error. Please check your connection."
            Checkout.INVALID_OPTIONS -> "Payment configuration error."
            else                    -> errorDescription ?: "Payment failed (code $errorCode)."
        }
        feeViewModel?.onPaymentFailed(message)
    }
}
