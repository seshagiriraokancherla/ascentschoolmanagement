package com.ascentschools.mobile.ui.announcements

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.AnnouncementDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class AnnouncementsUiState {
    object Loading : AnnouncementsUiState()
    data class Success(val announcements: List<AnnouncementDto>) : AnnouncementsUiState()
    data class Error(val message: String) : AnnouncementsUiState()
}

class AnnouncementsViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<AnnouncementsUiState>(AnnouncementsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init { load() }

    fun load() {
        _uiState.value = AnnouncementsUiState.Loading
        viewModelScope.launch {
            repo.getAnnouncements()
                .onSuccess { _uiState.value = AnnouncementsUiState.Success(it) }
                .onFailure { _uiState.value = AnnouncementsUiState.Error(it.message ?: "Failed to load announcements") }
        }
    }
}
