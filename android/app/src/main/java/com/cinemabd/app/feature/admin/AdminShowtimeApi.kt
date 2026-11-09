package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminShowtime
import com.cinemabd.app.feature.admin.model.AdminShowtimeGenerateRequest
import com.cinemabd.app.feature.admin.model.AdminShowtimeGenerateResult
import com.cinemabd.app.feature.admin.model.AdminShowtimeUpsertRequest
import com.cinemabd.app.feature.admin.model.ExpirePassedShowtimesResult
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminShowtimeApi {
    @GET("api/admin/showtimes")
    suspend fun getShowtimes(
        @Query("date") date: String? = null,
        @Query("roomId") roomId: String? = null
    ): ApiResponse<List<AdminShowtime>>

    @POST("api/admin/showtimes")
    suspend fun createShowtime(@Body request: AdminShowtimeUpsertRequest): ApiResponse<AdminShowtime>

    @PUT("api/admin/showtimes/{id}")
    suspend fun updateShowtime(
        @Path("id") showtimeId: String,
        @Body request: AdminShowtimeUpsertRequest
    ): ApiResponse<AdminShowtime>

    @DELETE("api/admin/showtimes/{id}")
    suspend fun deleteShowtime(@Path("id") showtimeId: String): ApiResponse<Unit>

    @POST("api/admin/showtimes/{id}/cancel")
    suspend fun cancelShowtime(@Path("id") showtimeId: String): ApiResponse<Unit>

    @POST("api/admin/showtimes/generate")
    suspend fun generateShowtimes(@Body request: AdminShowtimeGenerateRequest): ApiResponse<AdminShowtimeGenerateResult>

    @POST("api/admin/showtimes/expire-passed")
    suspend fun expirePassedShowtimes(): ApiResponse<ExpirePassedShowtimesResult>
}
