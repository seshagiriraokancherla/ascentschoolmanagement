package com.ascentschools.mobile.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.ChildDto
import com.ascentschools.mobile.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class SmsAuthState {
    object Idle             : SmsAuthState()
    object Loading          : SmsAuthState()
    object OtpSent          : SmsAuthState()   // show OTP entry screen
    object NeedsChildSelect : SmsAuthState()   // show child selector
    object ParentSuccess    : SmsAuthState()   // navigate to HomeScreen
    object TeacherSuccess   : SmsAuthState()   // navigate to TeacherHomeScreen
    data class Error(val message: String) : SmsAuthState()
}

class SmsAuthViewModel(private val repo: AuthRepository) : ViewModel() {

    private val _state    = MutableStateFlow<SmsAuthState>(SmsAuthState.Idle)
    val state = _state.asStateFlow()

    private val _children = MutableStateFlow<List<ChildDto>>(emptyList())
    val children = _children.asStateFlow()

    var pendingMobile: String = ""
        private set

    // ── Parent SMS OTP flow ───────────────────────────────────────────────────

    fun requestOtp(mobile: String) {
        if (mobile.isBlank() || mobile.length < 10) {
            _state.value = SmsAuthState.Error("Enter a valid 10-digit mobile number")
            return
        }
        pendingMobile = mobile.trim()
        _state.value  = SmsAuthState.Loading
        viewModelScope.launch {
            repo.requestOtp(pendingMobile)
                .onSuccess  { _state.value = SmsAuthState.OtpSent }
                .onFailure  { _state.value = SmsAuthState.Error(it.message ?: "Failed to send OTP") }
        }
    }

    fun verifyOtp(otp: String) {
        if (otp.length != 6 || !otp.all { it.isDigit() }) {
            _state.value = SmsAuthState.Error("Enter the 6-digit OTP")
            return
        }
        _state.value = SmsAuthState.Loading
        viewModelScope.launch {
            repo.verifyOtp(pendingMobile, otp)
                .onSuccess  {
                    // OTP verified — now load children for selection
                    loadChildren()
                }
                .onFailure  { _state.value = SmsAuthState.Error(it.message ?: "Invalid or expired OTP") }
        }
    }

    fun loadChildren() {
        viewModelScope.launch {
            repo.getChildren()
                .onSuccess { list ->
                    _children.value = list
                    _state.value    = SmsAuthState.NeedsChildSelect
                }
                .onFailure { _state.value = SmsAuthState.Error(it.message ?: "Failed to load children") }
        }
    }

    fun selectChild(linkId: Int) {
        _state.value = SmsAuthState.Loading
        viewModelScope.launch {
            repo.selectChild(linkId)
                .onSuccess  { _state.value = SmsAuthState.ParentSuccess }
                .onFailure  { _state.value = SmsAuthState.Error(it.message ?: "Failed to select child") }
        }
    }

    fun resendOtp() = requestOtp(pendingMobile)

    // ── Teacher login ─────────────────────────────────────────────────────────

    fun loginTeacher(username: String, password: String) {
        if (username.isBlank() || password.isBlank()) {
            _state.value = SmsAuthState.Error("Username and password are required")
            return
        }
        _state.value = SmsAuthState.Loading
        viewModelScope.launch {
            repo.loginTeacher(username, password)
                .onSuccess  { _state.value = SmsAuthState.TeacherSuccess }
                .onFailure  { _state.value = SmsAuthState.Error(it.message ?: "Login failed") }
        }
    }

    fun clearError() {
        if (_state.value is SmsAuthState.Error) _state.value = SmsAuthState.Idle
    }

    fun backToMobileEntry() {
        _state.value  = SmsAuthState.Idle
        pendingMobile = ""
    }
}
