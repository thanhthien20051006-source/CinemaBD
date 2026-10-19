package com.cinemabd.app.feature.auth

import com.cinemabd.app.core.database.CinemaDatabase
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApi,
    private val tokenDataStore: TokenDataStore,
    private val cinemaDatabase: CinemaDatabase,
    private val sessionManager: SessionManager
) {
    suspend fun login(request: LoginRequest): Result<AuthResponse> {
        return try {
            val response = authApi.login(request)
            if (response.success && response.data != null) {
                tokenDataStore.saveToken(response.data.token, isAdmin = false)
                sessionManager.resetSession()
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun adminLogin(request: LoginRequest): Result<AdminAuthResponse> {
        return try {
            val response = authApi.adminLogin(request)
            if (response.success && response.data != null) {
                val token = response.data.token
                cinemaDatabase.clearAllTables()
                tokenDataStore.saveToken(token, isAdmin = true)
                sessionManager.resetSession()
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
                tokenDataStore.saveToken(response.data.token, isAdmin = false)
                sessionManager.resetSession()
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
