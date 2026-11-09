package com.cinemabd.app

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.datastore.ThemePreferences
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.AuthState
import com.cinemabd.app.core.session.SessionManager
import com.cinemabd.app.navigation.Routes
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.onEach
import kotlinx.coroutines.flow.stateIn
import javax.inject.Inject

@HiltViewModel
class MainViewModel @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val themePreferences: ThemePreferences,
    val sessionManager: SessionManager
) : ViewModel() {

    val authState: StateFlow<AuthState> = tokenDataStore.authStateFlow
        .onEach { state ->
            if (state == AuthState.Unauthenticated && tokenDataStore.sessionFlow.first() != null) {
                tokenDataStore.clearSession()
            }
        }
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.Eagerly,
            initialValue = AuthState.Loading
        )

    val isLoading: StateFlow<Boolean> = authState
        .map { it == AuthState.Loading }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), true)

    val startDestination: StateFlow<String?> = authState
        .map { state ->
            when (state) {
                AuthState.Loading -> null
                AuthState.Unauthenticated -> Routes.LOGIN
                is AuthState.AuthenticatedAdmin -> Routes.ADMIN_DASHBOARD
                is AuthState.AuthenticatedCustomer -> Routes.CUSTOMER_HOME
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), null)

    val isAdmin: StateFlow<Boolean?> = authState
        .map { state ->
            when (state) {
                AuthState.Loading -> null
                is AuthState.AuthenticatedAdmin -> true
                is AuthState.AuthenticatedCustomer, AuthState.Unauthenticated -> false
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), null)

    val isDarkTheme: StateFlow<Boolean> = themePreferences.isDarkThemeFlow
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = true
        )
}
