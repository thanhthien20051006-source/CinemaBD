package com.cinemabd.app.feature.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.database.CinemaDatabase
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.session.SessionManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import javax.inject.Inject

@HiltViewModel
class MainScreenViewModel @Inject constructor(
    private val tokenDataStore: TokenDataStore,
    private val sessionManager: SessionManager,
    private val cinemaDatabase: CinemaDatabase
) : ViewModel() {
    val isAdmin: StateFlow<Boolean?> = tokenDataStore.isAdminFlow
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = null
        )

    val adminName: StateFlow<String?> = tokenDataStore.sessionFlow
        .map { it?.userName }
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = null
        )

    fun logout(onComplete: (() -> Unit)? = null) {
        viewModelScope.launch(NonCancellable) {
            sessionManager.clearSession()
            withContext(Dispatchers.IO) {
                cinemaDatabase.clearAllTables()
            }
            onComplete?.invoke()
        }
    }
}
