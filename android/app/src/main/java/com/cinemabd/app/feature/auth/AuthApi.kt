package com.cinemabd.app.feature.auth

import com.cinemabd.app.core.network.ApiResponse
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApi {
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): ApiResponse<AuthResponse>

    @POST("api/auth/register")
    suspend fun register(@Body request: RegisterRequest): ApiResponse<AuthResponse>
}
