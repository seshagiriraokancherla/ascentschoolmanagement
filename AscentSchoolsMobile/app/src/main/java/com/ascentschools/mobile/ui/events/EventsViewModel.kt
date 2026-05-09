package com.ascentschools.mobile.ui.events

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.SchoolEventDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class EventsUiState {
    object Loading : EventsUiState()
    data class Success(val events: List<SchoolEventDto>) : EventsUiState()
    data class Error(val message: String) : EventsUiState()
}

class EventsViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<EventsUiState>(EventsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init { load() }

    fun load() {
        _uiState.value = EventsUiState.Loading
        viewModelScope.launch {
            repo.getEvents()
                .onSuccess { _uiState.value = EventsUiState.Success(it) }
                .onFailure { _uiState.value = EventsUiState.Error(it.message ?: "Failed to load events") }
        }
    }
}
