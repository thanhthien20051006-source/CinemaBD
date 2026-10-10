package com.cinemabd.app.feature.booking

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.ComboResponse
import com.cinemabd.app.data.repository.CombosRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface ComboUiState {
    data object Loading : ComboUiState
    data object Empty : ComboUiState
    data class Error(val message: String) : ComboUiState
    data class Success(val combos: List<ComboResponse>) : ComboUiState
}

@HiltViewModel
class ComboViewModel @Inject constructor(
    private val combosRepository: CombosRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<ComboUiState>(ComboUiState.Loading)
    val uiState: StateFlow<ComboUiState> = _uiState.asStateFlow()

    init {
        loadCombos()
    }

    fun loadCombos() {
        viewModelScope.launch {
            _uiState.update { ComboUiState.Loading }
            val result = combosRepository.getCombos()
            result.onSuccess { combos ->
                if (combos.isEmpty()) {
                    _uiState.update { ComboUiState.Empty }
                } else {
                    _uiState.update { ComboUiState.Success(combos) }
                }
            }.onFailure { error ->
                _uiState.update {
                    ComboUiState.Error(error.message ?: "Không thể tải danh sách combo.")
                }
            }
        }
    }

    fun retry() {
        loadCombos()
    }
}
