package com.cinemabd.app.feature.auth

import com.cinemabd.app.core.datastore.TokenDataStore
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApi,
    private val tokenDataStore: TokenDataStore
) {
    suspend fun login(request: LoginRequest): Result<AuthResponse> {
        return try {
            val response = authApi.login(request)
            if (response.success && response.data != null) {
                tokenDataStore.saveToken(response.data.token)
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun register(request: RegisterRequest): Result<AuthResponse> {
        return try {
            val response = authApi.register(request)
            if (response.success && response.data != null) {
                tokenDataStore.saveToken(response.data.token)
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
