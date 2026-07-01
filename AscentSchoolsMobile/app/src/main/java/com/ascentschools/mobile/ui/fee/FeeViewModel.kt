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
    data class Success(val years: List<MobileFeeSummaryDto>) : FeeUiState()
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

    val categories = listOf("School", "Transport", "Hostel")

    private val _selectedCategory = MutableStateFlow("School")
    val selectedCategory: StateFlow<String> = _selectedCategory.asStateFlow()

    private val _uiState      = MutableStateFlow<FeeUiState>(FeeUiState.Loading)
    val uiState: StateFlow<FeeUiState> = _uiState.asStateFlow()

    private val _paymentState = MutableStateFlow<PaymentState>(PaymentState.Idle)
    val paymentState: StateFlow<PaymentState> = _paymentState.asStateFlow()

    init { loadFees() }

    fun selectCategory(category: String) {
        if (_selectedCategory.value == category) return
        _selectedCategory.value = category
        loadFees()
    }

    fun loadFees() {
        _uiState.value = FeeUiState.Loading
        viewModelScope.launch {
            repo.getOutstanding(_selectedCategory.value).fold(
                onSuccess = { _uiState.value = FeeUiState.Success(it) },
                onFailure = { _uiState.value = FeeUiState.Error(it.message ?: "Unknown error") }
            )
        }
    }

    /** Called when the user taps "Pay" on selected fee items from a single academic year. */
    fun initiatePayment(
        items          : List<MobileFeeLineItemDto>,
        academicYearId : Int,
        feeTypeCategory: String
    ) {
        val orderItems = items.map { li ->
            MobileFeeOrderItem(
                feeTypeId        = li.feeTypeId,
                termId           = li.termId,
                feePeriodId      = li.feePeriodId,
                busRouteId       = li.busRouteId,
                hostelId         = li.hostelId,
                // outstanding already nets out the concession; send gross + concession so the
                // server records net_amount = outstanding (correct paid total) while the
                // receipt still shows the concession line.
                amount           = li.outstanding.coerceAtLeast(0.0) + (li.concessionAmount),
                concessionAmount = li.concessionAmount
            )
        }
        val request = MobileCreateOrderRequest(
            academicYearId  = academicYearId,
            feeTypeCategory = feeTypeCategory,
            items           = orderItems
        )
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
            repo.verifyPayment(
                gatewayOrderId,
                MobileVerifyRequest(gatewayOrderId, paymentId, orderId, signature)
            ).fold(
                onSuccess = {
                    _paymentState.value = PaymentState.Success(it)
                    loadFees()
                },
                onFailure = { _paymentState.value = PaymentState.Failed(it.message ?: "Verification failed") }
            )
        }
    }

    /** Called by MainActivity when Razorpay returns an error or the user cancels. */
    fun onPaymentFailed(message: String) {
        _paymentState.value = PaymentState.Failed(message)
    }

    fun resetPaymentState() {
        _paymentState.value = PaymentState.Idle
    }

    /** Fetches a receipt's detail (for in-app print / save-as-PDF) and calls back. */
    fun fetchReceipt(id: Int, onReady: (ReceiptDetailDto) -> Unit, onError: (String) -> Unit) {
        viewModelScope.launch {
            repo.getReceiptDetail(id).fold(
                onSuccess = { onReady(it) },
                onFailure = { onError(it.message ?: "Failed to load receipt") }
            )
        }
    }
}
