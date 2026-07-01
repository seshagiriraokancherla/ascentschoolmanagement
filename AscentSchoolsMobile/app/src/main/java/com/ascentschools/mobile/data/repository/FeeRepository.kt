package com.ascentschools.mobile.data.repository

import com.ascentschools.mobile.data.api.*
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import retrofit2.Response

class FeeRepository(private val api: ApiService) {

    suspend fun getOutstanding(feeTypeCategory: String): Result<List<MobileFeeSummaryDto>> {
        return runCatching {
            val resp = api.getOutstanding(feeTypeCategory)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Failed to load fees")
            body.data.years
        }
    }

    suspend fun createOrder(request: MobileCreateOrderRequest): Result<MobileOrderResponse> {
        return runCatching {
            val resp = api.createPaymentOrder(request)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Failed to create payment order")
            body.data
        }
    }

    suspend fun verifyPayment(
        gatewayOrderId : Int,
        request        : MobileVerifyRequest
    ): Result<MobilePaymentResultDto> {
        return runCatching {
            val resp = api.verifyPayment(gatewayOrderId, request)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Payment verification failed")
            body.data
        }
    }

    suspend fun getReceiptDetail(id: Int): Result<ReceiptDetailDto> {
        return runCatching {
            val resp = api.getReceiptDetail(id)
            val body = resp.bodyOrError()
            if (!body.success || body.data == null) error(body.message ?: "Failed to load receipt")
            body.data
        }
    }

    private fun <T> Response<T>.bodyOrError(): T {
        if (isSuccessful) return body() ?: error("Empty response body")
        val errJson = errorBody()?.string()
        val errMsg  = try {
            val type = object : TypeToken<ApiResponse<Any>>() {}.type
            Gson().fromJson<ApiResponse<Any>>(errJson, type)?.message
        } catch (_: Exception) { null }
        error(errMsg ?: "Server error ${code()}")
    }
}
