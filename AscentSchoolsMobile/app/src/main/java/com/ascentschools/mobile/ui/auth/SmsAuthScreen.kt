package com.ascentschools.mobile.ui.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import com.ascentschools.mobile.AscentApp
import com.ascentschools.mobile.BuildConfig
import com.ascentschools.mobile.R
import com.ascentschools.mobile.ui.theme.NavyBlue
import com.ascentschools.mobile.ui.theme.NavyBlueLight

// ── Gradient used on the login background ────────────────────────────────────

private val LoginGradient = Brush.linearGradient(
    colors = listOf(Color(0xFF1E3A8A), Color(0xFF1D4ED8), Color(0xFF0369A1)),
    start  = Offset(0f, 0f),
    end    = Offset(Float.POSITIVE_INFINITY, Float.POSITIVE_INFINITY)
)

// ── Frosted card surface ──────────────────────────────────────────────────────

private val FrostedWhite = Color(0xF5FFFFFF)   // 96 % white — sits over gradient

@Composable
fun SmsAuthScreen(
    viewModel      : SmsAuthViewModel,
    onParentSuccess: () -> Unit,
    onTeacherSuccess: () -> Unit,
    onChangeSchool : (() -> Unit)? = null   // generic single-app build only
) {
    val state    by viewModel.state.collectAsState()
    val children by viewModel.children.collectAsState()
    val context  = LocalContext.current

    val snackbarHost = remember { SnackbarHostState() }

    LaunchedEffect(state) {
        when (state) {
            is SmsAuthState.ParentSuccess  -> onParentSuccess()
            is SmsAuthState.TeacherSuccess -> onTeacherSuccess()
            is SmsAuthState.Error -> {
                snackbarHost.showSnackbar((state as SmsAuthState.Error).message)
                viewModel.clearError()
            }
            else -> {}
        }
    }

    var showTeacherSheet by remember { mutableStateOf(false) }

    // Generic single app shows the selected school's branding (loaded from /branding);
    // per-school flavors use the baked app_name + launcher icon.
    val isGeneric   = BuildConfig.SCHOOL_CODE.isBlank()
    val tokenStore  = remember { (context.applicationContext as AscentApp).tokenStore }
    val bakedName   = context.getString(R.string.app_name)
    val appName     = if (isGeneric) (tokenStore.brandingName?.takeIf { it.isNotBlank() } ?: bakedName) else bakedName
    val logoModel: Any = if (isGeneric && !tokenStore.brandingLogoUrl.isNullOrBlank())
                             tokenStore.brandingLogoUrl!!
                         else
                             context.packageManager.getApplicationIcon(context.packageName)

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        containerColor = Color.Transparent
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(LoginGradient)
                .padding(padding),
            contentAlignment = Alignment.Center
        ) {
            // Decorative blurred circle — top-right
            Box(
                modifier = Modifier
                    .size(280.dp)
                    .offset(x = 120.dp, y = (-100).dp)
                    .align(Alignment.TopEnd)
                    .background(Color(0x33FFFFFF), CircleShape)
                    .blur(60.dp)
            )
            // Decorative blurred circle — bottom-left
            Box(
                modifier = Modifier
                    .size(200.dp)
                    .offset(x = (-80).dp, y = 80.dp)
                    .align(Alignment.BottomStart)
                    .background(Color(0x22FFFFFF), CircleShape)
                    .blur(50.dp)
            )

            Column(
                modifier            = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Spacer(Modifier.height(56.dp))

                // ── Logo with glow ring ───────────────────────────────────────
                Box(contentAlignment = Alignment.Center) {
                    // Soft glow behind logo
                    Box(
                        modifier = Modifier
                            .size(112.dp)
                            .background(Color(0x40FFFFFF), CircleShape)
                            .blur(16.dp)
                    )
                    Box(
                        modifier = Modifier
                            .size(100.dp)
                            .background(Color(0x30FFFFFF), CircleShape)
                    )
                    AsyncImage(
                        model              = logoModel,
                        contentDescription = appName,
                        modifier           = Modifier
                            .size(84.dp)
                            .clip(CircleShape)
                            .shadow(8.dp, CircleShape)
                    )
                }

                Spacer(Modifier.height(16.dp))

                Text(
                    text       = appName,
                    style      = MaterialTheme.typography.titleLarge,
                    color      = Color.White,
                    textAlign  = TextAlign.Center
                )
                Spacer(Modifier.height(4.dp))
                Text(
                    text  = "Parent Portal",
                    style = MaterialTheme.typography.bodyMedium,
                    color = Color.White.copy(alpha = 0.75f)
                )

                Spacer(Modifier.height(36.dp))

                // ── Auth card ─────────────────────────────────────────────────
                when (state) {
                    is SmsAuthState.NeedsChildSelect -> ChildSelectorCard(
                        children  = children,
                        isLoading = state is SmsAuthState.Loading,
                        onSelect  = { viewModel.selectChild(it) }
                    )
                    is SmsAuthState.OtpSent -> OtpEntryCard(
                        mobile    = viewModel.pendingMobile,
                        isLoading = false,
                        onVerify  = { viewModel.verifyOtp(it) },
                        onResend  = { viewModel.resendOtp() },
                        onBack    = { viewModel.backToMobileEntry() }
                    )
                    else -> MobileEntryCard(
                        isLoading = state is SmsAuthState.Loading,
                        onSubmit  = { viewModel.requestOtp(it) }
                    )
                }

                if (state !is SmsAuthState.NeedsChildSelect) {
                    Spacer(Modifier.height(24.dp))
                    TextButton(onClick = { showTeacherSheet = true }) {
                        Text(
                            "Staff Login",
                            style = MaterialTheme.typography.labelLarge,
                            color = Color.White.copy(alpha = 0.65f)
                        )
                    }
                    // Generic single app: let the parent re-pick their school
                    if (isGeneric && onChangeSchool != null) {
                        TextButton(onClick = onChangeSchool) {
                            Text(
                                "Change School",
                                style = MaterialTheme.typography.labelLarge,
                                color = Color.White.copy(alpha = 0.65f)
                            )
                        }
                    }
                }

                Spacer(Modifier.height(24.dp))

                Text(
                    text  = "v${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",
                    style = MaterialTheme.typography.labelSmall,
                    color = Color.White.copy(alpha = 0.35f)
                )

                Spacer(Modifier.height(16.dp))
            }
        }
    }

    if (showTeacherSheet) {
        TeacherLoginSheet(
            isLoading = state is SmsAuthState.Loading,
            onLogin   = { u, p -> viewModel.loginTeacher(u, p) },
            onDismiss = { showTeacherSheet = false }
        )
    }
}

// ── Shared frosted card wrapper ───────────────────────────────────────────────

@Composable
private fun FrostedCard(content: @Composable ColumnScope.() -> Unit) {
    Card(
        shape  = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(containerColor = FrostedWhite),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
        modifier = Modifier
            .fillMaxWidth()
            .shadow(24.dp, RoundedCornerShape(20.dp), ambientColor = Color(0x30000000)),
        content = {
            Column(
                modifier            = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(16.dp),
                content             = content
            )
        }
    )
}

// ── Step 1: Mobile number entry ───────────────────────────────────────────────

@Composable
private fun MobileEntryCard(isLoading: Boolean, onSubmit: (String) -> Unit) {
    var mobile by remember { mutableStateOf("") }

    FrostedCard {
        Text(
            "Enter your mobile number",
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurface
        )
        Text(
            "We'll send a one-time verification code",
            style     = MaterialTheme.typography.bodySmall,
            textAlign = TextAlign.Center,
            color     = MaterialTheme.colorScheme.onSurfaceVariant
        )

        OutlinedTextField(
            value           = mobile,
            onValueChange   = { if (it.length <= 10) mobile = it.filter { c -> c.isDigit() } },
            label           = { Text("Mobile Number") },
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
            modifier        = Modifier.fillMaxWidth(),
            singleLine      = true,
            prefix          = { Text("+91 ") },
            shape           = RoundedCornerShape(12.dp)
        )

        Button(
            onClick  = { onSubmit(mobile) },
            enabled  = mobile.length == 10 && !isLoading,
            modifier = Modifier.fillMaxWidth().height(52.dp),
            shape    = RoundedCornerShape(12.dp),
            colors   = ButtonDefaults.buttonColors(containerColor = NavyBlue)
        ) {
            if (isLoading) CircularProgressIndicator(
                modifier    = Modifier.size(20.dp),
                strokeWidth = 2.dp,
                color       = Color.White
            )
            else Text("Send OTP", style = MaterialTheme.typography.labelLarge, color = Color.White)
        }
    }
}

// ── Step 2: OTP entry ─────────────────────────────────────────────────────────

@Composable
private fun OtpEntryCard(
    mobile    : String,
    isLoading : Boolean,
    onVerify  : (String) -> Unit,
    onResend  : () -> Unit,
    onBack    : () -> Unit
) {
    var otp by remember { mutableStateOf("") }

    FrostedCard {
        Text(
            "Verify your number",
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onSurface
        )
        Text(
            "OTP sent to +91 $mobile",
            style     = MaterialTheme.typography.bodySmall,
            textAlign = TextAlign.Center,
            color     = MaterialTheme.colorScheme.onSurfaceVariant
        )

        OutlinedTextField(
            value           = otp,
            onValueChange   = { if (it.length <= 6) otp = it.filter { c -> c.isDigit() } },
            label           = { Text("6-digit OTP") },
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
            modifier        = Modifier.fillMaxWidth(),
            singleLine      = true,
            textStyle       = LocalTextStyle.current.copy(letterSpacing = 8.sp, fontSize = 22.sp),
            shape           = RoundedCornerShape(12.dp)
        )

        Button(
            onClick  = { onVerify(otp) },
            enabled  = otp.length == 6 && !isLoading,
            modifier = Modifier.fillMaxWidth().height(52.dp),
            shape    = RoundedCornerShape(12.dp),
            colors   = ButtonDefaults.buttonColors(containerColor = NavyBlue)
        ) {
            if (isLoading) CircularProgressIndicator(
                modifier    = Modifier.size(20.dp),
                strokeWidth = 2.dp,
                color       = Color.White
            )
            else Text("Verify & Login", style = MaterialTheme.typography.labelLarge, color = Color.White)
        }

        Row(
            modifier              = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            TextButton(onClick = onBack)   { Text("Change number") }
            TextButton(onClick = onResend, enabled = !isLoading) { Text("Resend OTP") }
        }
    }
}

// ── Child selector ────────────────────────────────────────────────────────────

@Composable
private fun ChildSelectorCard(
    children  : List<com.ascentschools.mobile.data.api.ChildDto>,
    isLoading : Boolean,
    onSelect  : (Int) -> Unit
) {
    Column(
        modifier            = Modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            "Select Child",
            style = MaterialTheme.typography.headlineSmall,
            color = Color.White
        )
        Text(
            "Choose the child you want to view",
            style     = MaterialTheme.typography.bodySmall,
            textAlign = TextAlign.Center,
            color     = Color.White.copy(alpha = 0.75f)
        )
        Spacer(Modifier.height(4.dp))

        if (isLoading || children.isEmpty()) {
            CircularProgressIndicator(color = Color.White)
        } else {
            children.forEach { child ->
                Card(
                    shape  = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = FrostedWhite),
                    modifier = Modifier
                        .fillMaxWidth()
                        .shadow(12.dp, RoundedCornerShape(16.dp), ambientColor = Color(0x20000000))
                        .clickable { onSelect(child.linkId) }
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Box(
                            modifier = Modifier
                                .size(44.dp)
                                .background(NavyBlue.copy(alpha = 0.12f), CircleShape),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(
                                text  = child.studentName.take(1).uppercase(),
                                style = MaterialTheme.typography.titleMedium,
                                color = NavyBlue
                            )
                        }
                        Spacer(Modifier.width(12.dp))
                        Column {
                            Text(
                                child.studentName,
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.onSurface
                            )
                            Text(
                                "${child.className} · ${child.admissionNo}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
            }
        }
    }
}

// ── Teacher login bottom sheet ────────────────────────────────────────────────

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun TeacherLoginSheet(
    isLoading : Boolean,
    onLogin   : (String, String) -> Unit,
    onDismiss : () -> Unit
) {
    var username        by remember { mutableStateOf("") }
    var password        by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }

    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("Staff Login", style = MaterialTheme.typography.titleLarge)
            Text(
                "Use your school system credentials",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            OutlinedTextField(
                value         = username,
                onValueChange = { username = it },
                label         = { Text("Username") },
                modifier      = Modifier.fillMaxWidth(),
                singleLine    = true,
                shape         = RoundedCornerShape(12.dp)
            )

            OutlinedTextField(
                value                = password,
                onValueChange        = { password = it },
                label                = { Text("Password") },
                visualTransformation = if (passwordVisible) VisualTransformation.None
                                       else PasswordVisualTransformation(),
                keyboardOptions      = KeyboardOptions(keyboardType = KeyboardType.Password),
                trailingIcon = {
                    IconButton(onClick = { passwordVisible = !passwordVisible }) {
                        Icon(
                            imageVector        = if (passwordVisible) Icons.Default.VisibilityOff
                                                else Icons.Default.Visibility,
                            contentDescription = if (passwordVisible) "Hide" else "Show"
                        )
                    }
                },
                modifier   = Modifier.fillMaxWidth(),
                singleLine = true,
                shape      = RoundedCornerShape(12.dp)
            )

            Button(
                onClick  = { onLogin(username, password) },
                enabled  = username.isNotBlank() && password.isNotBlank() && !isLoading,
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape    = RoundedCornerShape(12.dp),
                colors   = ButtonDefaults.buttonColors(containerColor = NavyBlue)
            ) {
                if (isLoading) CircularProgressIndicator(
                    modifier    = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                    color       = Color.White
                )
                else Text("Login", style = MaterialTheme.typography.labelLarge, color = Color.White)
            }
        }
    }
}
