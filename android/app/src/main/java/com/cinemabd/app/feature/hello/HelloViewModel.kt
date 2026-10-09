package com.cinemabd.app.feature.hello

import androidx.lifecycle.ViewModel
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject

@HiltViewModel
class HelloViewModel @Inject constructor() : ViewModel() {

    private val _uiState = MutableStateFlow(HelloUiState())
    val uiState: StateFlow<HelloUiState> = _uiState.asStateFlow()

    // Example function to update state
    fun setLoading() {
        _uiState.value = HelloUiState(isLoading = true, isSuccess = false)
    }
}
