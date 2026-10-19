package com.cinemabd.app

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.datastore.ThemePreferences
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import com.cinemabd.app.navigation.Routes
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class MainViewModel @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val themePreferences: ThemePreferences,
    val sessionManager: SessionManager
) : ViewModel() {

    private val _isLoading = MutableStateFlow(true)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _startDestination = MutableStateFlow<String?>(null)
    val startDestination: StateFlow<String?> = _startDestination.asStateFlow()

    val isAdmin: StateFlow<Boolean?> = tokenDataStore.isAdminFlow
        .map<Boolean, Boolean?> { it }
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = null
        )

    val isDarkTheme: StateFlow<Boolean> = themePreferences.isDarkThemeFlow
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = true
        )

    init {
        viewModelScope.launch {
            val token = tokenDataStore.tokenFlow.firstOrNull()
            _startDestination.value = if (token.isNullOrEmpty()) {
                Routes.AUTH
            } else {
                Routes.MAIN
            }
            _isLoading.value = false
        }
    }
}
