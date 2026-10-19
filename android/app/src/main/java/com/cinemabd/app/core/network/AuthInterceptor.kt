package com.cinemabd.app.core.network

import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import kotlinx.coroutines.flow.firstOrNull
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

        // We use runBlocking here because Interceptor is synchronous and OkHttp
        // calls this on a background thread.
        val token = runBlocking { tokenDataStore.tokenFlow.firstOrNull() }

        val requestBuilder = originalRequest.newBuilder()
        if (!token.isNullOrEmpty()) {
            requestBuilder.header("Authorization", "Bearer $token")
        }

        val response = chain.proceed(requestBuilder.build())

        // If an authenticated request returns 401 Unauthorized, automatically clear session
        // and notify session expired (excluding auth requests like login/register)
        if (response.code == 401 && !token.isNullOrEmpty() && !isAuthEndpoint(path)) {
            runBlocking {
                sessionManager.clearSession()
                sessionManager.emitSessionExpired()
            }
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
