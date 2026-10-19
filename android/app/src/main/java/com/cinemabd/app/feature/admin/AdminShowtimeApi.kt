package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminShowtime
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminShowtimeApi {
    @GET("api/admin/showtimes")
    suspend fun getShowtimes(@Query("date") date: String): ApiResponse<List<AdminShowtime>>

    @POST("api/admin/showtimes/{id}/cancel")
    suspend fun cancelShowtime(@Path("id") showtimeId: String): ApiResponse<Unit>
}
