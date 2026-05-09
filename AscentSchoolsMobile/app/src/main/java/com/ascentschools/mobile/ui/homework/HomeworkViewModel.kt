package com.ascentschools.mobile.ui.homework

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.HomeworkDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class HomeworkUiState {
    object Loading : HomeworkUiState()
    data class Success(val homework: List<HomeworkDto>) : HomeworkUiState()
    data class Error(val message: String) : HomeworkUiState()
}

class HomeworkViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<HomeworkUiState>(HomeworkUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init { load() }

    fun load() {
        _uiState.value = HomeworkUiState.Loading
        viewModelScope.launch {
            repo.getHomework()
                .onSuccess { _uiState.value = HomeworkUiState.Success(it) }
                .onFailure { _uiState.value = HomeworkUiState.Error(it.message ?: "Failed to load homework") }
        }
    }
}
