package com.cinemabd.app.feature.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.datastore.TokenDataStore
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class MainScreenViewModel @Inject constructor(
    private val tokenDataStore: TokenDataStore
) : ViewModel() {
    val isAdmin: StateFlow<Boolean?> = tokenDataStore.isAdminFlow
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5000),
            initialValue = null
        )

    fun logout(onComplete: () -> Unit) {
        viewModelScope.launch {
            tokenDataStore.clearToken()
            onComplete()
        }
    }
}
