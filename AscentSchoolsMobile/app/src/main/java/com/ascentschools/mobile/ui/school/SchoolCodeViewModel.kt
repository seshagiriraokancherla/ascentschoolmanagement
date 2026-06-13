package com.ascentschools.mobile.ui.school

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.SchoolByCodeDto
import com.ascentschools.mobile.data.local.TokenStore
import com.ascentschools.mobile.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class SchoolCodeState {
    object Idle : SchoolCodeState()
    object Loading : SchoolCodeState()
    data class Confirm(val school: SchoolByCodeDto) : SchoolCodeState()
    object Done : SchoolCodeState()
    data class Error(val message: String) : SchoolCodeState()
}

/**
 * First-launch school resolution for the generic single app:
 * enter 4-digit code → resolve → confirm school name → store subdomain (+ branding).
 */
class SchoolCodeViewModel(
    private val repo: AuthRepository,
    private val tokenStore: TokenStore
) : ViewModel() {

    private val _state = MutableStateFlow<SchoolCodeState>(SchoolCodeState.Idle)
    val state = _state.asStateFlow()

    fun resolve(code: String) {
        val c = code.trim()
        if (c.length != 4 || !c.all { it.isDigit() }) {
            _state.value = SchoolCodeState.Error("Enter the 4-digit school code.")
            return
        }
        _state.value = SchoolCodeState.Loading
        viewModelScope.launch {
            repo.getSchoolByCode(c)
                .onSuccess { _state.value = SchoolCodeState.Confirm(it) }
                .onFailure { _state.value = SchoolCodeState.Error(it.message ?: "Invalid school code") }
        }
    }

    fun confirm(school: SchoolByCodeDto) {
        _state.value = SchoolCodeState.Loading
        viewModelScope.launch {
            // Persist the resolved subdomain; subsequent requests send it as X-School-Code.
            tokenStore.schoolCode = school.schoolCode
            // Best-effort branding load (uses the just-set school header); failure is non-fatal.
            repo.loadBranding()
            _state.value = SchoolCodeState.Done
        }
    }

    fun reset() { _state.value = SchoolCodeState.Idle }
}
