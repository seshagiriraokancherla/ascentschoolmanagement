package com.ascentschools.mobile.ui.fee

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ascentschools.mobile.data.api.*
import com.ascentschools.mobile.data.repository.FeeRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

// ── UI state ──────────────────────────────────────────────────────────────────

sealed class FeeUiState {
    object Loading : FeeUiState()
    data class Success(val summary: MobileFeeSummaryDto) : FeeUiState()
    data class Error(val message: String) : FeeUiState()
}

sealed class PaymentState {
    object Idle : PaymentState()
    object CreatingOrder : PaymentState()
    /** Order created — Activity should open Razorpay checkout. */
    data class OrderReady(val order: MobileOrderResponse) : PaymentState()
    object Verifying : PaymentState()
    data class Success(val result: MobilePaymentResultDto) : PaymentState()
    data class Failed(val message: String) : PaymentState()
}

// ─────────────────────────────────────────────────────────────────────────────

class FeeViewModel(private val repo: FeeRepository) : ViewModel() {

    private val _uiState      = MutableStateFlow<FeeUiState>(FeeUiState.Loading)
    val uiState: StateFlow<FeeUiState> = _uiState.asStateFlow()

    private val _paymentState = MutableStateFlow<PaymentState>(PaymentState.Idle)
    val paymentState: StateFlow<PaymentState> = _paymentState.asStateFlow()

    // Holds the selected items for the pending order (used after Razorpay callback)
    private var pendingOrderRequest: MobileCreateOrderRequest? = null

    init { loadFees() }

    fun loadFees() {
        _uiState.value = FeeUiState.Loading
        viewModelScope.launch {
            repo.getFees().fold(
                onSuccess = { _uiState.value = FeeUiState.Success(it) },
                onFailure = { _uiState.value = FeeUiState.Error(it.message ?: "Unknown error") }
            )
        }
    }

    /** Called when the user taps "Pay" on selected fee items. */
    fun initiatePayment(items: List<MobileFeeLineItemDto>, academicYearId: Int, paymentModeId: Int) {
        val orderItems = items.map { li ->
            MobileFeeOrderItem(
                feeTypeId        = li.feeTypeId ?: 0,
                termId           = li.termId,
                amount           = li.outstanding,
                concessionAmount = 0.0
            )
        }
        val request = MobileCreateOrderRequest(
            academicYearId = academicYearId,
            paymentModeId  = paymentModeId,
            items          = orderItems
        )
        pendingOrderRequest = request
        _paymentState.value = PaymentState.CreatingOrder

        viewModelScope.launch {
            repo.createOrder(request).fold(
                onSuccess = { _paymentState.value = PaymentState.OrderReady(it) },
                onFailure = { _paymentState.value = PaymentState.Failed(it.message ?: "Failed to create order") }
            )
        }
    }

    /** Called by MainActivity after Razorpay returns success. */
    fun verifyPayment(gatewayOrderId: Int, paymentId: String, orderId: String, signature: String) {
        _paymentState.value = PaymentState.Verifying
        viewModelScope.launch {
            repo.verifyPayment(MobileVerifyRequest(gatewayOrderId, paymentId, orderId, signature)).fold(
                onSuccess = {
                    _paymentState.value = PaymentState.Success(it)
                    loadFees() // Refresh fee list
                },
                onFailure = { _paymentState.value = PaymentState.Failed(it.message ?: "Verification failed") }
            )
        }
    }

    /** Called by MainActivity when Razorpay returns an error/cancellation. */
    fun onPaymentFailed(message: String) {
        _paymentState.value = PaymentState.Failed(message)
    }

    fun resetPaymentState() {
        _paymentState.value = PaymentState.Idle
        pendingOrderRequest = null
    }
}
