package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.ComboResponse
import retrofit2.http.GET
import retrofit2.http.Path

interface CombosApi {
    @GET("api/combos")
    suspend fun getCombos(): ApiResponse<List<ComboResponse>>

    @GET("api/combos/{id}")
    suspend fun getComboById(@Path("id") id: String): ApiResponse<ComboResponse>
}
