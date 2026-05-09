package com.ascentschools.mobile.ui.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.ChildDto

private enum class AuthMode  { Parent, Teacher }
private enum class ParentFlow { Login, Register }

// ── Main entry ────────────────────────────────────────────────────────────────

@Composable
fun LoginScreen(
    viewModel      : LoginViewModel,
    onLoginSuccess : () -> Unit,
    onTeacherLogin : () -> Unit = onLoginSuccess
) {
    val uiState  by viewModel.uiState.collectAsState()
    val children by viewModel.children.collectAsState()
    val context  = LocalContext.current

    var authMode by remember { mutableStateOf(AuthMode.Parent) }

    val snackbarHost = remember { SnackbarHostState() }
    LaunchedEffect(uiState) {
        if (uiState is LoginUiState.Error) {
            snackbarHost.showSnackbar((uiState as LoginUiState.Error).message)
            viewModel.clearError()
        }
    }
    LaunchedEffect(uiState) {
        if (uiState is LoginUiState.Success)        onLoginSuccess()
        if (uiState is LoginUiState.TeacherSuccess) onTeacherLogin()
    }

    val appName = context.getString(com.ascentschools.mobile.R.string.app_name)

    Scaffold(snackbarHost = { SnackbarHost(snackbarHost) }) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.background)
                .padding(padding),
            contentAlignment = Alignment.Center
        ) {
            when {
                uiState is LoginUiState.NeedsChildSelect ->
                    ChildSelectorPanel(
                        children  = children,
                        isLoading = false,
                        onSelect  = { viewModel.selectChild(it) },
                        onLoad    = { viewModel.loadChildren() }
                    )

                else -> Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(24.dp)
                        .verticalScroll(rememberScrollState()),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Spacer(Modifier.height(40.dp))

                    Text(
                        text       = appName,
                        fontSize   = 26.sp,
                        fontWeight = FontWeight.Bold,
                        color      = MaterialTheme.colorScheme.primary
                    )
                    Text(
                        text     = if (authMode == AuthMode.Parent) "Parent Portal" else "Teacher Portal",
                        fontSize = 13.sp,
                        color    = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.6f)
                    )

                    Spacer(Modifier.height(24.dp))

                    // Parent / Teacher tabs
                    TabRow(selectedTabIndex = authMode.ordinal) {
                        Tab(
                            selected = authMode == AuthMode.Parent,
                            onClick  = { authMode = AuthMode.Parent },
                            text     = { Text("Parent") }
                        )
                        Tab(
                            selected = authMode == AuthMode.Teacher,
                            onClick  = { authMode = AuthMode.Teacher },
                            text     = { Text("Teacher") }
                        )
                    }

                    Spacer(Modifier.height(20.dp))

                    if (authMode == AuthMode.Parent) {
                        ParentAuthPanel(
                            viewModel = viewModel,
                            isLoading = uiState is LoginUiState.Loading
                        )
                    } else {
                        TeacherAuthPanel(
                            viewModel = viewModel,
                            isLoading = uiState is LoginUiState.Loading
                        )
                    }

                    Spacer(Modifier.height(40.dp))
                }
            }
        }
    }
}

// ── Teacher panel ─────────────────────────────────────────────────────────────

@Composable
private fun TeacherAuthPanel(viewModel: LoginViewModel, isLoading: Boolean) {
    var username        by remember { mutableStateOf("") }
    var password        by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }

    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(20.dp)) {
            Text("Teacher Login", fontWeight = FontWeight.SemiBold, fontSize = 16.sp)
            Spacer(Modifier.height(16.dp))
            OutlinedTextField(
                value         = username,
                onValueChange = { username = it },
                label         = { Text("Username") },
                modifier      = Modifier.fillMaxWidth(),
                singleLine    = true
            )
            Spacer(Modifier.height(10.dp))
            PinField(
                value    = password,
                visible  = passwordVisible,
                onChange = { password = it },
                onToggle = { passwordVisible = !passwordVisible },
                label    = "Password"
            )
            Spacer(Modifier.height(16.dp))
            ActionButton("Login", isLoading) {
                viewModel.loginTeacher(username, password)
            }
        }
    }
}

// ── Parent panel ──────────────────────────────────────────────────────────────

@Composable
private fun ParentAuthPanel(viewModel: LoginViewModel, isLoading: Boolean) {
    var flow       by remember { mutableStateOf(ParentFlow.Login) }
    var fullName   by remember { mutableStateOf("") }
    var mobile     by remember { mutableStateOf("") }
    var pin        by remember { mutableStateOf("") }
    var pinVisible by remember { mutableStateOf(false) }

    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(20.dp)) {
            when (flow) {
                ParentFlow.Login -> {
                    Text("Parent Login", fontWeight = FontWeight.SemiBold, fontSize = 16.sp)
                    Spacer(Modifier.height(16.dp))
                    OutlinedTextField(
                        value           = mobile,
                        onValueChange   = { mobile = it },
                        label           = { Text("Mobile Number") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                        modifier        = Modifier.fillMaxWidth(),
                        singleLine      = true
                    )
                    Spacer(Modifier.height(10.dp))
                    PinField(pin, pinVisible, { pin = it }, { pinVisible = !pinVisible })
                    Spacer(Modifier.height(16.dp))
                    ActionButton("Login", isLoading) {
                        viewModel.loginParent(mobile, pin)
                    }
                    Spacer(Modifier.height(10.dp))
                    LinkText("Register as Parent") { flow = ParentFlow.Register }
                }

                ParentFlow.Register -> {
                    Text("Parent Register", fontWeight = FontWeight.SemiBold, fontSize = 16.sp)
                    Spacer(Modifier.height(16.dp))
                    OutlinedTextField(
                        value         = fullName,
                        onValueChange = { fullName = it },
                        label         = { Text("Full Name") },
                        modifier      = Modifier.fillMaxWidth(),
                        singleLine    = true
                    )
                    Spacer(Modifier.height(10.dp))
                    OutlinedTextField(
                        value           = mobile,
                        onValueChange   = { mobile = it },
                        label           = { Text("Mobile Number") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                        modifier        = Modifier.fillMaxWidth(),
                        singleLine      = true
                    )
                    Spacer(Modifier.height(10.dp))
                    PinField(pin, pinVisible, { pin = it }, { pinVisible = !pinVisible })
                    Spacer(Modifier.height(16.dp))
                    ActionButton("Register", isLoading) {
                        viewModel.registerParent(fullName, mobile, pin)
                    }
                    Spacer(Modifier.height(10.dp))
                    LinkText("Back to Login") { flow = ParentFlow.Login }
                }
            }
        }
    }
}

// ── Child selector ────────────────────────────────────────────────────────────

@Composable
private fun ChildSelectorPanel(
    children  : List<ChildDto>,
    isLoading : Boolean,
    onSelect  : (Int) -> Unit,
    onLoad    : () -> Unit
) {
    LaunchedEffect(Unit) { onLoad() }

    Column(
        modifier            = Modifier
            .fillMaxWidth()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text("Select Child", fontWeight = FontWeight.Bold, fontSize = 20.sp)
        Text(
            "Choose the child you want to view",
            fontSize  = 13.sp,
            color     = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.6f),
            textAlign = TextAlign.Center
        )
        Spacer(Modifier.height(24.dp))

        if (isLoading || children.isEmpty()) {
            CircularProgressIndicator()
        } else {
            children.forEach { child ->
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 6.dp)
                        .clickable { onSelect(child.linkId) },
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Column(Modifier.padding(16.dp)) {
                        Text(child.studentName, fontWeight = FontWeight.SemiBold)
                        Text(
                            "${child.className} · ${child.admissionNo}",
                            fontSize = 12.sp,
                            color    = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                        )
                    }
                }
            }
        }
    }
}

// ── Reusable widgets ──────────────────────────────────────────────────────────

@Composable
private fun PinField(
    value    : String,
    visible  : Boolean,
    onChange : (String) -> Unit,
    onToggle : () -> Unit,
    label    : String = "PIN"
) {
    OutlinedTextField(
        value                = value,
        onValueChange        = onChange,
        label                = { Text(label) },
        visualTransformation = if (visible) VisualTransformation.None else PasswordVisualTransformation(),
        keyboardOptions      = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
        trailingIcon         = {
            IconButton(onClick = onToggle) {
                Icon(
                    imageVector        = if (visible) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                    contentDescription = if (visible) "Hide PIN" else "Show PIN"
                )
            }
        },
        modifier   = Modifier.fillMaxWidth(),
        singleLine = true
    )
}

@Composable
private fun ActionButton(label: String, isLoading: Boolean, onClick: () -> Unit) {
    Button(
        onClick  = onClick,
        enabled  = !isLoading,
        modifier = Modifier
            .fillMaxWidth()
            .height(48.dp)
    ) {
        if (isLoading) {
            CircularProgressIndicator(
                color       = MaterialTheme.colorScheme.onPrimary,
                modifier    = Modifier.size(20.dp),
                strokeWidth = 2.dp
            )
        } else {
            Text(label)
        }
    }
}

@Composable
private fun LinkText(text: String, onClick: () -> Unit) {
    Text(
        text     = text,
        color    = MaterialTheme.colorScheme.primary,
        fontSize = 13.sp,
        modifier = Modifier.clickable(onClick = onClick)
    )
}
