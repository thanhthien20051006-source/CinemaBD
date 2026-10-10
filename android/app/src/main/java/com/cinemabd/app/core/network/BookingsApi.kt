package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.CheckoutRequest
import com.cinemabd.app.core.network.model.CheckoutResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.model.SeatHoldRequest
import com.cinemabd.app.core.network.model.SeatHoldResult
import com.cinemabd.app.core.network.model.SeatResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface BookingsApi {
    @GET("api/bookings/showtimes/{showtimeId}/seats")
    suspend fun getSeats(@Path("showtimeId") showtimeId: String): ApiResponse<List<SeatResponse>>

    @POST("api/bookings/hold-seats")
    suspend fun holdSeats(@Body request: SeatHoldRequest): ApiResponse<SeatHoldResult>

    @POST("api/bookings/checkout")
    suspend fun checkout(
        @Header("X-Return-Url") returnUrl: String? = null,
        @Body request: CheckoutRequest
    ): ApiResponse<CheckoutResponse>

    @GET("api/bookings/invoice/{txnRef}")
    suspend fun getInvoice(@Path("txnRef") txnRef: String): ApiResponse<InvoiceResponse>
}
