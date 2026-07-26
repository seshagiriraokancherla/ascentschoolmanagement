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
import androidx.compose.runtime.*
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import com.ascentschools.mobile.data.api.MobileOrderResponse
import com.ascentschools.mobile.data.api.RetrofitClient
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
                var isLoggedIn by remember { mutableStateOf(tokenStore.isLoggedIn) }
                var userType   by remember { mutableStateOf(tokenStore.userType) }

                // Generic single app (SCHOOL_CODE empty): the parent must pick a school
                // (4-digit code) before anything else. Baked flavors skip this entirely.
                val isGenericApp = BuildConfig.SCHOOL_CODE.isBlank()
                var needsSchool by remember {
                    mutableStateOf(isGenericApp && tokenStore.schoolCode.isNullOrBlank())
                }

                // App update is handled by Play In-App Updates (see `updater` above),
                // which overlays its own UI outside Compose — no gate state needed here.
                // A FLEXIBLE update that finished downloading surfaces a restart prompt below.

                // Cold start does NO blocking auth work, and CANNOT log the user out.
                //
                // This used to await a token refresh behind a spinner and call
                // tokenStore.clear() on ANY failure — so no network yet (the common case
                // when the phone has just woken), a timeout, or a transient 5xx wiped a
                // perfectly valid session and sent the parent back to the OTP screen. That
                // was the real cause of the daily re-login, and it defeated every
                // server-side token-lifetime fix.
                //
                // Nothing here needs to succeed first: the stored access token already
                // carries child context (select-child writes it), it is long-lived
                // (Jwt:MobileAccessTokenMins), and a mid-session expiry is handled by the
                // OkHttp Authenticator, which retries the call and never clears state.
                //
                // The refresh below is opportunistic only — it slides the server's 365-day
                // refresh window forward. It runs after the UI is up and its failure is a
                // no-op.
                LaunchedEffect(Unit) {
                    if (tokenStore.isLoggedIn) {
                        registerPush(pushRepo)
                        requestNotifPermissionIfNeeded()
                        launch { runCatching { authRepo.refreshSessionQuietly() } }
                    }
                }

                // The one and only automatic logout: a live API call 401'd and the server
                // then explicitly rejected the refresh token, so the session is genuinely
                // dead (revoked, or unused for 365 days). Without this the user would sit
                // in a working shell with every screen failing and no way back to login.
                val sessionExpired by RetrofitClient.sessionExpired.collectAsState()
                LaunchedEffect(sessionExpired) {
                    if (sessionExpired && isLoggedIn) {
                        tokenStore.clear()
                        isLoggedIn = false
                        userType   = ""
                        RetrofitClient.sessionExpired.value = false
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

                if (needsSchool) {
                    val schoolVm = remember { SchoolCodeViewModel(authRepo, tokenStore) }
                    SchoolCodeScreen(viewModel = schoolVm, onResolved = { needsSchool = false })
                } else if (isLoggedIn && userType == "teacher") {
                    val teacherVm = remember { TeacherViewModel(teacherRepo) }
                    var teacherScreen by remember { mutableStateOf<TeacherScreen>(TeacherScreen.Home) }

                    when (val screen = teacherScreen) {
                        is TeacherScreen.Home -> TeacherHomeScreen(
                            teacherName  = tokenStore.studentName ?: "",
                            viewModel    = teacherVm,
                            tokenStore   = tokenStore,
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
