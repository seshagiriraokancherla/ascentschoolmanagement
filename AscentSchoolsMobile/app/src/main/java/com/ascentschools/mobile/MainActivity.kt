package com.ascentschools.mobile

import android.os.Build
import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.annotation.RequiresApi
import androidx.compose.runtime.*
import com.ascentschools.mobile.data.api.MobileOrderResponse
import com.ascentschools.mobile.data.repository.AuthRepository
import com.ascentschools.mobile.data.repository.FeeRepository
import com.ascentschools.mobile.data.repository.StudentRepository
import com.ascentschools.mobile.data.repository.TeacherRepository
import com.ascentschools.mobile.ui.auth.SmsAuthScreen
import com.ascentschools.mobile.ui.auth.SmsAuthViewModel
import com.ascentschools.mobile.ui.fee.FeeViewModel
import com.ascentschools.mobile.ui.fee.PaymentState
import com.ascentschools.mobile.ui.home.HomeScreen
import com.ascentschools.mobile.ui.teacher.TeacherAttendanceScreen
import com.ascentschools.mobile.ui.teacher.TeacherHomeScreen
import com.ascentschools.mobile.ui.teacher.TeacherHomeworkScreen
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
    data class Attendance(val classId: Int, val sectionId: Int) : TeacherScreen()
    data class Homework(val classId: Int) : TeacherScreen()
}

class MainActivity : ComponentActivity(), PaymentResultWithDataListener {

    // Lazily-created ViewModel — set once user is logged in
    private var feeViewModel: FeeViewModel? = null

    @RequiresApi(Build.VERSION_CODES.O)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        // Pre-load Razorpay resources in background for faster first-open
        Checkout.preload(applicationContext)

        val app         = application as AscentApp
        val tokenStore  = app.tokenStore
        val api         = com.ascentschools.mobile.data.api.RetrofitClient.apiService
        val authRepo    = AuthRepository(api, tokenStore)
        val studentRepo = StudentRepository(api)
        val feeRepo     = FeeRepository(api)
        val teacherRepo = TeacherRepository(api)

        setContent {
            AscentTheme {
                var isLoggedIn by remember { mutableStateOf(tokenStore.isLoggedIn) }
                var userType   by remember { mutableStateOf(tokenStore.userType) }

                if (isLoggedIn && userType == "teacher") {
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
                            onLogout = {
                                CoroutineScope(Dispatchers.IO).launch {
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
                    }

                } else if (isLoggedIn) {
                    // Parent home
                    val feeVm = remember {
                        FeeViewModel(feeRepo).also { feeViewModel = it }
                    }

                    HomeScreen(
                        repo       = studentRepo,
                        feeVm      = feeVm,
                        tokenStore = tokenStore,
                        onInitiatePayment = { items, academicYearId, paymentModeId ->
                            feeVm.initiatePayment(items, academicYearId, paymentModeId)
                        },
                        onLogout = {
                            CoroutineScope(Dispatchers.IO).launch {
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

                } else {
                    val authVm = remember { SmsAuthViewModel(authRepo) }
                    SmsAuthScreen(
                        viewModel        = authVm,
                        onParentSuccess  = { isLoggedIn = true; userType = tokenStore.userType },
                        onTeacherSuccess = { isLoggedIn = true; userType = "teacher" }
                    )
                }
            }
        }
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
