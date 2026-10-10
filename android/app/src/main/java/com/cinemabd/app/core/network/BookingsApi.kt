package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.SeatHoldRequest
import com.cinemabd.app.core.network.model.SeatHoldResult
import com.cinemabd.app.core.network.model.SeatResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface BookingsApi {
    @GET("api/bookings/showtimes/{showtimeId}/seats")
    suspend fun getSeats(@Path("showtimeId") showtimeId: String): ApiResponse<List<SeatResponse>>

    @POST("api/bookings/hold-seats")
    suspend fun holdSeats(@Body request: SeatHoldRequest): ApiResponse<SeatHoldResult>
}
