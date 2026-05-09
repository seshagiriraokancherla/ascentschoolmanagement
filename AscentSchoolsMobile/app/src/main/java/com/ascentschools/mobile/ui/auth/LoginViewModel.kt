package com.ascentschools.mobile.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.ChildDto
import com.ascentschools.mobile.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class LoginUiState {
    object Idle             : LoginUiState()
    object Loading          : LoginUiState()
    object Success          : LoginUiState()          // parent after child selected, or teacher
    object TeacherSuccess   : LoginUiState()          // teacher login complete
    object NeedsChildSelect : LoginUiState()
    data class Error(val message: String) : LoginUiState()
}

class LoginViewModel(private val repo: AuthRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<LoginUiState>(LoginUiState.Idle)
    val uiState = _uiState.asStateFlow()

    private val _children = MutableStateFlow<List<ChildDto>>(emptyList())
    val children = _children.asStateFlow()

    // ── Parent login / register ───────────────────────────────────────────────

    fun loginParent(mobile: String, pin: String) {
        if (mobile.isBlank() || pin.length < 4) {
            _uiState.value = LoginUiState.Error("Mobile number and a 4-digit PIN are required")
            return
        }
        _uiState.value = LoginUiState.Loading
        viewModelScope.launch {
            repo.loginParent(mobile, pin)
                .onSuccess { _uiState.value = LoginUiState.NeedsChildSelect }
                .onFailure { _uiState.value = LoginUiState.Error(it.message ?: "Login failed") }
        }
    }

    fun registerParent(fullName: String, mobile: String, pin: String) {
        if (fullName.isBlank() || mobile.isBlank() || pin.length < 4) {
            _uiState.value = LoginUiState.Error("Please fill all fields (PIN must be 4+ digits)")
            return
        }
        _uiState.value = LoginUiState.Loading
        viewModelScope.launch {
            repo.registerParent(fullName, mobile, pin)
                .onSuccess { _uiState.value = LoginUiState.NeedsChildSelect }
                .onFailure { _uiState.value = LoginUiState.Error(it.message ?: "Registration failed") }
        }
    }

    fun loadChildren() {
        viewModelScope.launch {
            repo.getChildren()
                .onSuccess { _children.value = it }
                .onFailure { _uiState.value = LoginUiState.Error(it.message ?: "Failed to load children") }
        }
    }

    fun selectChild(linkId: Int) {
        _uiState.value = LoginUiState.Loading
        viewModelScope.launch {
            repo.selectChild(linkId)
                .onSuccess { _uiState.value = LoginUiState.Success }
                .onFailure { _uiState.value = LoginUiState.Error(it.message ?: "Failed") }
        }
    }

    // ── Teacher login ─────────────────────────────────────────────────────────

    fun loginTeacher(username: String, password: String) {
        if (username.isBlank() || password.isBlank()) {
            _uiState.value = LoginUiState.Error("Username and password are required")
            return
        }
        _uiState.value = LoginUiState.Loading
        viewModelScope.launch {
            repo.loginTeacher(username, password)
                .onSuccess { _uiState.value = LoginUiState.TeacherSuccess }
                .onFailure { _uiState.value = LoginUiState.Error(it.message ?: "Login failed") }
        }
    }

    fun clearError() { _uiState.value = LoginUiState.Idle }
}
