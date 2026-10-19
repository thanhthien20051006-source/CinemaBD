package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminCombo
import com.cinemabd.app.feature.admin.model.AdminComboRequest
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface AdminComboApi {
    @GET("api/admin/combos")
    suspend fun getCombos(): ApiResponse<List<AdminCombo>>

    @GET("api/admin/combos/{id}")
    suspend fun getCombo(@Path("id") id: String): ApiResponse<AdminCombo>

    @POST("api/admin/combos")
    suspend fun createCombo(@Body request: AdminComboRequest): ApiResponse<AdminCombo>

    @PUT("api/admin/combos/{id}")
    suspend fun updateCombo(
        @Path("id") id: String,
        @Body request: AdminComboRequest
    ): ApiResponse<AdminCombo>

    @DELETE("api/admin/combos/{id}")
    suspend fun deleteCombo(@Path("id") id: String): ApiResponse<Unit>
}
