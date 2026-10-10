package com.cinemabd.app.data.repository

import com.cinemabd.app.core.network.PaymentsApi
import com.cinemabd.app.core.network.model.PaymentCallbackResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class PaymentsRepository @Inject constructor(
    private val paymentsApi: PaymentsApi
) {
    suspend fun demoConfirm(txnRef: String, gateway: String = "MOMO"): Result<PaymentCallbackResponse> = withContext(Dispatchers.IO) {
        try {
            val response = paymentsApi.demoConfirm(txnRef, gateway)
            val result = response.data
            if (response.success && result != null && result.success) {
                Result.success(result)
            } else {
                val errorMsg = result?.message?.takeIf { it.isNotBlank() }
                    ?: response.message.takeIf { it.isNotBlank() }
                    ?: "Xác nhận thanh toán demo thất bại"
                Result.failure(Exception(errorMsg))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun handleVnPayReturn(queryMap: Map<String, String>): Result<PaymentCallbackResponse> = withContext(Dispatchers.IO) {
        try {
            val response = paymentsApi.vnPayReturn(queryMap)
            val result = response.data
            if (response.success && result != null && result.success) {
                Result.success(result)
            } else {
                val errorMsg = result?.message?.takeIf { it.isNotBlank() }
                    ?: response.message.takeIf { it.isNotBlank() }
                    ?: "Thanh toán VNPay không thành công"
                Result.failure(Exception(errorMsg))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
