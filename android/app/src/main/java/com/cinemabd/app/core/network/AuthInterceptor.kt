package com.cinemabd.app.core.network

import com.cinemabd.app.core.datastore.TokenDataStore
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val authEventBus: AuthEventBus
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

        // If an authenticated request returns 401 Unauthorized, automatically invalidate token
        // and notify app to return to Login (excluding auth requests like login/register)
        if (response.code == 401 && !token.isNullOrEmpty() &&
            !path.contains("/api/account/login") && !path.contains("/api/account/register")
        ) {
            runBlocking {
                tokenDataStore.clearToken()
            }
            authEventBus.emitSessionExpired()
        }

        return response
    }
}
