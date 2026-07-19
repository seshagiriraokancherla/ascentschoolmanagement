package com.ascentschools.mobile.ui.messages

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.ParentThreadViewDto
import com.ascentschools.mobile.data.repository.StudentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed class MessagesUiState {
    object Loading : MessagesUiState()
    data class Success(val view: ParentThreadViewDto) : MessagesUiState()
    data class Error(val message: String) : MessagesUiState()
}

class MessagesViewModel(private val repo: StudentRepository) : ViewModel() {

    private val _uiState = MutableStateFlow<MessagesUiState>(MessagesUiState.Loading)
    val uiState = _uiState.asStateFlow()

    /** Set while a send/report/block call is in flight — drives the send button spinner. */
    private val _isBusy = MutableStateFlow(false)
    val isBusy = _isBusy.asStateFlow()

    /** One-shot toast text (report confirmed, send failed, ...). */
    private val _toast = MutableStateFlow<String?>(null)
    val toast = _toast.asStateFlow()

    init { load() }

    fun load() {
        _uiState.value = MessagesUiState.Loading
        viewModelScope.launch {
            repo.getMessageThread()
                .onSuccess {
                    _uiState.value = MessagesUiState.Success(it)
                    // Opening the thread marks the teacher's messages read.
                    if (it.threadId != null) repo.markMessagesRead()
                }
                .onFailure { _uiState.value = MessagesUiState.Error(it.message ?: "Failed to load messages") }
        }
    }

    /** Refresh without the full-screen spinner — used after send/block so the list doesn't flash. */
    private fun reload() {
        viewModelScope.launch {
            repo.getMessageThread().onSuccess { _uiState.value = MessagesUiState.Success(it) }
        }
    }

    fun send(text: String) {
        if (text.isBlank()) return
        viewModelScope.launch {
            _isBusy.value = true
            repo.sendMessage(text.trim())
                .onSuccess { reload() }
                .onFailure { _toast.value = it.message ?: "Send failed" }
            _isBusy.value = false
        }
    }

    fun report(messageId: Int, reason: String?) {
        viewModelScope.launch {
            _isBusy.value = true
            repo.reportMessage(messageId, reason)
                .onSuccess { _toast.value = "Reported. The school will review this message." }
                .onFailure { _toast.value = it.message ?: "Report failed" }
            _isBusy.value = false
        }
    }

    fun setBlocked(blocked: Boolean) {
        viewModelScope.launch {
            _isBusy.value = true
            val result = if (blocked) repo.blockMessageThread() else repo.unblockMessageThread()
            result
                .onSuccess {
                    _toast.value = if (blocked) "Conversation blocked." else "Conversation unblocked."
                    reload()
                }
                .onFailure { _toast.value = it.message ?: "Failed" }
            _isBusy.value = false
        }
    }

    fun clearToast() { _toast.value = null }
}
