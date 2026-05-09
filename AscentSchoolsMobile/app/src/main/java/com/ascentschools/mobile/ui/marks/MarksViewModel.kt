package com.ascentschools.mobile.ui.marks

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.MarksResultDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class MarksUiState {
    object Loading : MarksUiState()
    data class Success(val results: List<MarksResultDto>) : MarksUiState()
    data class Error(val message: String) : MarksUiState()
}

class MarksViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<MarksUiState>(MarksUiState.Loading)
    val uiState = _uiState.asStateFlow()

    // 0 = load with default academic year (0 means current in backend)
    private val _academicYearId = MutableStateFlow(0)
    val academicYearId = _academicYearId.asStateFlow()

    init { load() }

    fun setAcademicYear(id: Int) {
        _academicYearId.value = id
        load()
    }

    fun load() {
        _uiState.value = MarksUiState.Loading
        viewModelScope.launch {
            repo.getMarks(_academicYearId.value)
                .onSuccess { _uiState.value = MarksUiState.Success(it) }
                .onFailure { _uiState.value = MarksUiState.Error(it.message ?: "Failed to load marks") }
        }
    }
}
