package com.cinemabd.app.data.repository

import com.cinemabd.app.core.network.BookingsApi
import com.cinemabd.app.core.network.model.CheckoutRequest
import com.cinemabd.app.core.network.model.CheckoutResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.model.SeatHoldRequest
import com.cinemabd.app.core.network.model.SeatHoldResult
import com.cinemabd.app.core.network.model.SeatResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BookingsRepository @Inject constructor(
    private val bookingsApi: BookingsApi
) {
    suspend fun getSeats(showtimeId: String): Result<List<SeatResponse>> = withContext(Dispatchers.IO) {
        try {
            val response = bookingsApi.getSeats(showtimeId)
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tải sơ đồ ghế" }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun holdSeats(showtimeId: String, seats: List<String>): Result<SeatHoldResult> = withContext(Dispatchers.IO) {
        try {
            val response = bookingsApi.holdSeats(SeatHoldRequest(showtimeId = showtimeId, seats = seats))
            val result = response.data
            if (response.success && result != null && result.success) {
                Result.success(result)
            } else {
                val errorMsg = result?.message?.takeIf { it.isNotBlank() }
                    ?: response.message.takeIf { it.isNotBlank() }
                    ?: "Không thể giữ ghế. Vui lòng chọn ghế khác."
                Result.failure(Exception(errorMsg))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun checkout(
        showtimeId: String,
        seats: List<String>,
        combos: String?,
        totalAmount: Double,
        returnUrl: String? = null
    ): Result<CheckoutResponse> = withContext(Dispatchers.IO) {
        try {
            val request = CheckoutRequest(
                showtimeId = showtimeId,
                seats = seats,
                combos = combos,
                totalAmount = totalAmount
            )
            val response = bookingsApi.checkout(returnUrl = returnUrl, request = request)
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tạo phiên thanh toán" }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getInvoice(txnRef: String): Result<InvoiceResponse> = withContext(Dispatchers.IO) {
        try {
            val response = bookingsApi.getInvoice(txnRef)
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không tìm thấy hóa đơn" }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
