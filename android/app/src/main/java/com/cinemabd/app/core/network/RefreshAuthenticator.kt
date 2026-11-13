package com.cinemabd.app.core.network

import com.cinemabd.app.core.datastore.TokenDataStore
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import kotlinx.serialization.Serializable
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class RefreshAuthenticator @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val refreshApi: RefreshApi
) : Authenticator {
    override fun authenticate(route: Route?, response: Response): Request? {
        if (responseCount(response) >= 2) return null
        return runBlocking {
            val session = tokenDataStore.sessionFlow.first() ?: return@runBlocking null
            val refreshed = try {
                val refreshRequest = if (session.role == com.cinemabd.app.core.session.UserRole.Admin) {
                    AdminRefreshRequest(session.accessToken, session.refreshToken ?: return@runBlocking null)
                } else {
                    RefreshRequest(session.accessToken, session.refreshToken ?: return@runBlocking null)
                }
                refreshApi.refresh(refreshRequest, session.role == com.cinemabd.app.core.session.UserRole.Admin)
            } catch (_: Exception) {
                return@runBlocking null
            }
            val data = refreshed.data ?: return@runBlocking null
            val current = tokenDataStore.sessionFlow.first() ?: return@runBlocking null
            if (current.accessToken != session.accessToken) return@runBlocking response.request.newBuilder()
                .header("Authorization", "Bearer ${current.accessToken}").build()
            tokenDataStore.saveSession(session.copy(
                accessToken = data.token,
                refreshToken = data.refreshToken,
                expiresAtUtcMillis = com.cinemabd.app.core.utils.JwtUtils.parseExpirationUtcMillis(data.token) ?: 0L
            ))
            response.request.newBuilder().header("Authorization", "Bearer ${data.token}").build()
        }
    }

    private fun responseCount(response: Response): Int {
        var count = 1
        var prior = response.priorResponse
        while (prior != null) { count++; prior = prior.priorResponse }
        return count
    }
}

@Serializable
data class RefreshRequest(val accessToken: String, val refreshToken: String)

@Serializable
data class RefreshResponse(val token: String, val refreshToken: String, val sessionId: String)

@Serializable
data class AdminRefreshRequest(val accessToken: String, val refreshToken: String)

interface RefreshApi {
    @retrofit2.http.POST("api/auth/refresh")
    suspend fun refreshCustomer(@retrofit2.http.Body request: RefreshRequest): ApiResponse<RefreshResponse>

    @retrofit2.http.POST("api/admin/auth/refresh")
    suspend fun refreshAdmin(@retrofit2.http.Body request: AdminRefreshRequest): ApiResponse<RefreshResponse>

    suspend fun refresh(request: Any, admin: Boolean): ApiResponse<RefreshResponse> =
        if (admin) refreshAdmin(request as AdminRefreshRequest) else refreshCustomer(request as RefreshRequest)
}
