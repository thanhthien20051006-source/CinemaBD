package com.cinemabd.app.core.network

import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val sessionManager: SessionManager
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()
        val path = originalRequest.url.encodedPath

        // If it's an authentication endpoint (login, register), don't attach Bearer token or check session
        if (isAuthEndpoint(path)) {
            return chain.proceed(originalRequest)
        }

        // OkHttp interceptors are synchronous; read the latest persisted access token here.
        val token = runBlocking { tokenDataStore.tokenFlow.first() }

        val requestBuilder = originalRequest.newBuilder()
        if (!token.isNullOrEmpty()) {
            requestBuilder.header("Authorization", "Bearer $token")
        }

        val response = chain.proceed(requestBuilder.build())

        // If an authenticated request returns 401 Unauthorized, clear session and notify expiry.
        if (response.code == 401 && !token.isNullOrEmpty()) {
            sessionManager.notifySessionExpiredAsync()
        }

        return response
    }

    companion object {
        fun isAuthEndpoint(path: String): Boolean {
            val normalized = path.trimStart('/')
            return normalized.contains("api/auth/login") ||
                   normalized.contains("api/admin/auth/login") ||
                   normalized.contains("api/auth/register")
        }
    }
}
