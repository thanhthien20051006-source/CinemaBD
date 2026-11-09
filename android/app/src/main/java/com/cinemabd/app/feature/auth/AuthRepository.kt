package com.cinemabd.app.feature.auth

import com.cinemabd.app.core.database.CinemaDatabase
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import com.cinemabd.app.core.session.UserRole
import com.cinemabd.app.core.session.UserSession
import com.cinemabd.app.core.utils.JwtUtils
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApi,
    private val tokenDataStore: TokenDataStore,
    private val cinemaDatabase: CinemaDatabase,
    private val sessionManager: SessionManager
) {
    suspend fun login(request: LoginRequest): Result<AuthResponse> = withContext(Dispatchers.IO) {
        try {
            val response = authApi.login(request)
            if (response.success && response.data != null) {
                val data = response.data
                val resolvedData = data.copy(role = UserRole.Customer.value)
                tokenDataStore.saveSession(resolvedData.toUserSession(UserRole.Customer, data.email))
                sessionManager.resetSession()
                Result.success(resolvedData)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun adminLogin(request: LoginRequest): Result<AdminAuthResponse> = withContext(Dispatchers.IO) {
        try {
            val response = authApi.adminLogin(request)
            if (response.success && response.data != null) {
                val token = response.data.token
                cinemaDatabase.clearAllTables()
                val roleFromToken = JwtUtils.parseRole(token)
                val roleString = response.data.role?.takeIf { it.isNotBlank() } ?: roleFromToken ?: UserRole.Admin.value
                tokenDataStore.saveSession(
                    UserSession(
                        accessToken = token,
                        userId = response.data.adminId.takeIf { it > 0 }?.toString() ?: JwtUtils.parseSubject(token),
                        userName = response.data.fullName.ifBlank { response.data.username },
                        role = UserRole.Admin,
                        expiresAtUtcMillis = JwtUtils.parseExpirationUtcMillis(token) ?: 0L
                    )
                )
                sessionManager.resetSession()
                Result.success(response.data.copy(role = roleString))
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun register(request: RegisterRequest): Result<AuthResponse> = withContext(Dispatchers.IO) {
        try {
            val response = authApi.register(request)
            if (response.success && response.data != null) {
                val data = response.data
                val resolvedData = data.copy(role = UserRole.Customer.value)
                tokenDataStore.saveSession(resolvedData.toUserSession(UserRole.Customer, data.email ?: request.email))
                sessionManager.resetSession()
                Result.success(resolvedData)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    private fun AuthResponse.toUserSession(role: UserRole, email: String?): UserSession = UserSession(
        accessToken = token,
        refreshToken = refreshToken,
        userId = userId.ifBlank { JwtUtils.parseSubject(token) },
        userName = fullName.ifBlank { username },
        email = email,
        role = role,
        expiresAtUtcMillis = JwtUtils.parseExpirationUtcMillis(token) ?: 0L
    )
}
