package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminRoom
import retrofit2.http.GET
import retrofit2.http.Query

interface AdminRoomApi {
    @GET("api/admin/rooms")
    suspend fun getRooms(@Query("cinemaId") cinemaId: String? = null): ApiResponse<List<AdminRoom>>
}
