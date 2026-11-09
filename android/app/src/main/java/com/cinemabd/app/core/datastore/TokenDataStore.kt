package com.cinemabd.app.core.datastore

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import com.cinemabd.app.core.session.AuthState
import com.cinemabd.app.core.session.UserRole
import com.cinemabd.app.core.session.UserSession
import com.cinemabd.app.core.utils.JwtUtils
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
import java.util.concurrent.atomic.AtomicReference
import javax.inject.Inject
import javax.inject.Singleton

val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "token_prefs")

@Singleton
class TokenDataStore @Inject constructor(
    @ApplicationContext private val context: Context
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val TOKEN_KEY = stringPreferencesKey("jwt_token")
    private val REFRESH_TOKEN_KEY = stringPreferencesKey("refresh_token")
    private val USER_ID_KEY = stringPreferencesKey("user_id")
    private val USER_NAME_KEY = stringPreferencesKey("user_name")
    private val EMAIL_KEY = stringPreferencesKey("email")
    private val ROLE_KEY = stringPreferencesKey("user_role")
    private val EXPIRY_KEY = longPreferencesKey("expires_at_utc_millis")
    private val IS_ADMIN_KEY = booleanPreferencesKey("is_admin")
    private val cachedToken = AtomicReference<String?>(null)

    val tokenFlow: Flow<String?> = context.dataStore.data
        .map { preferences -> preferences[TOKEN_KEY] }

    val refreshTokenFlow: Flow<String?> = context.dataStore.data
        .map { preferences -> preferences[REFRESH_TOKEN_KEY] }

    val userIdFlow: Flow<String?> = context.dataStore.data
        .map { preferences -> preferences[USER_ID_KEY] }

    val roleFlow: Flow<String?> = context.dataStore.data
        .map { preferences -> preferences[ROLE_KEY] }

    val isAdminFlow: Flow<Boolean> = context.dataStore.data
        .map { preferences -> preferences[ROLE_KEY] == UserRole.Admin.value || preferences[IS_ADMIN_KEY] == true }

    val sessionFlow: Flow<UserSession?> = context.dataStore.data
        .map { preferences ->
            val token = preferences[TOKEN_KEY]?.takeIf(String::isNotBlank) ?: return@map null
            val role = UserRole.from(preferences[ROLE_KEY])
                ?: if (preferences[IS_ADMIN_KEY] == true) UserRole.Admin else UserRole.Customer
            val expiry = preferences[EXPIRY_KEY] ?: JwtUtils.parseExpirationUtcMillis(token) ?: 0L
            UserSession(
                accessToken = token,
                refreshToken = preferences[REFRESH_TOKEN_KEY],
                userId = preferences[USER_ID_KEY] ?: JwtUtils.parseSubject(token),
                userName = preferences[USER_NAME_KEY],
                email = preferences[EMAIL_KEY],
                role = role,
                expiresAtUtcMillis = expiry
            )
        }

    val authStateFlow: Flow<AuthState> = sessionFlow
        .map { session ->
            when {
                session == null -> AuthState.Unauthenticated
                session.expiresAtUtcMillis <= 0L || session.expiresAtUtcMillis <= System.currentTimeMillis() -> AuthState.Unauthenticated
                session.role == UserRole.Admin -> AuthState.AuthenticatedAdmin(session)
                else -> AuthState.AuthenticatedCustomer(session)
            }
        }

    init {
        scope.launch {
            tokenFlow.collectLatest { token ->
                cachedToken.set(token)
            }
        }
    }

    fun getCachedToken(): String? = cachedToken.get()

    suspend fun saveSession(session: UserSession) {
        cachedToken.set(session.accessToken)
        context.dataStore.edit { preferences ->
            preferences[TOKEN_KEY] = session.accessToken
            session.refreshToken?.let { preferences[REFRESH_TOKEN_KEY] = it } ?: preferences.remove(REFRESH_TOKEN_KEY)
            session.userId?.let { preferences[USER_ID_KEY] = it } ?: preferences.remove(USER_ID_KEY)
            session.userName?.let { preferences[USER_NAME_KEY] = it } ?: preferences.remove(USER_NAME_KEY)
            session.email?.let { preferences[EMAIL_KEY] = it } ?: preferences.remove(EMAIL_KEY)
            preferences[ROLE_KEY] = session.role.value
            preferences[EXPIRY_KEY] = session.expiresAtUtcMillis
            preferences[IS_ADMIN_KEY] = session.role == UserRole.Admin
        }
    }

    suspend fun saveToken(token: String, isAdmin: Boolean = false) {
        val role = if (isAdmin) UserRole.Admin else UserRole.Customer
        saveSession(
            UserSession(
                accessToken = token,
                userId = JwtUtils.parseSubject(token),
                userName = null,
                role = role,
                expiresAtUtcMillis = JwtUtils.parseExpirationUtcMillis(token) ?: 0L
            )
        )
    }

    suspend fun clearSession() {
        cachedToken.set(null)
        context.dataStore.edit { preferences ->
            preferences.remove(TOKEN_KEY)
            preferences.remove(REFRESH_TOKEN_KEY)
            preferences.remove(USER_ID_KEY)
            preferences.remove(USER_NAME_KEY)
            preferences.remove(EMAIL_KEY)
            preferences.remove(ROLE_KEY)
            preferences.remove(EXPIRY_KEY)
            preferences.remove(IS_ADMIN_KEY)
        }
    }

    suspend fun clearToken() = clearSession()
}
