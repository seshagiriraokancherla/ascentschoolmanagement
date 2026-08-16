package com.ascentschools.mobile.ui.calendar

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.CalendarEventDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.Calendar

sealed class CalendarUiState {
    object Loading : CalendarUiState()
    // year + month travel with the events so the almanac list always matches the loaded month.
    data class Success(val year: Int, val month: Int, val events: List<CalendarEventDto>) : CalendarUiState()
    data class Error(val message: String) : CalendarUiState()
}

private val MONTH_NAMES = arrayOf(
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
)

class CalendarViewModel(private val repo: StudentRepository) : ViewModel() {

    // 1-based month
    private var year  = Calendar.getInstance().get(Calendar.YEAR)
    private var month = Calendar.getInstance().get(Calendar.MONTH) + 1

    private val _uiState = MutableStateFlow<CalendarUiState>(CalendarUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _monthLabel = MutableStateFlow(label())
    val monthLabel = _monthLabel.asStateFlow()

    init { load() }

    fun load() {
        _uiState.value = CalendarUiState.Loading
        val y = year; val m = month   // capture so a fast month-switch can't mismatch the result
        viewModelScope.launch {
            repo.getCalendar(m, y)
                .onSuccess { _uiState.value = CalendarUiState.Success(y, m, it) }
                .onFailure { _uiState.value = CalendarUiState.Error(it.message ?: "Failed to load calendar") }
        }
    }

    fun prevMonth() {
        if (--month < 1) { month = 12; year-- }
        _monthLabel.value = label()
        load()
    }

    fun nextMonth() {
        if (++month > 12) { month = 1; year++ }
        _monthLabel.value = label()
        load()
    }

    private fun label() = "${MONTH_NAMES[month - 1]} $year"
}
