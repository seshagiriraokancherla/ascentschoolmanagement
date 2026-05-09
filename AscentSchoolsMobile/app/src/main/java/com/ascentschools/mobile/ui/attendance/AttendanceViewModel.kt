package com.ascentschools.mobile.ui.attendance

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.AttendanceSummaryDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.Calendar

sealed class AttendanceUiState {
    object Idle    : AttendanceUiState()
    object Loading : AttendanceUiState()
    data class Success(val summary: AttendanceSummaryDto) : AttendanceUiState()
    data class Error(val message: String) : AttendanceUiState()
}

class AttendanceViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<AttendanceUiState>(AttendanceUiState.Idle)
    val uiState = _uiState.asStateFlow()

    private val cal = Calendar.getInstance()
    private val _selectedMonth = MutableStateFlow(cal.get(Calendar.MONTH) + 1)
    private val _selectedYear  = MutableStateFlow(cal.get(Calendar.YEAR))
    val selectedMonth = _selectedMonth.asStateFlow()
    val selectedYear  = _selectedYear.asStateFlow()

    init { load() }

    fun setMonthYear(month: Int, year: Int) {
        _selectedMonth.value = month
        _selectedYear.value  = year
        load()
    }

    fun load() {
        _uiState.value = AttendanceUiState.Loading
        viewModelScope.launch {
            repo.getAttendance(_selectedMonth.value, _selectedYear.value)
                .onSuccess { _uiState.value = AttendanceUiState.Success(it) }
                .onFailure { _uiState.value = AttendanceUiState.Error(it.message ?: "Failed to load attendance") }
        }
    }
}
