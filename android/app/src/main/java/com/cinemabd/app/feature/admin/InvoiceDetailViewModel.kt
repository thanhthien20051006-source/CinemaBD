package com.cinemabd.app.feature.admin

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class InvoiceDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val adminRepository: AdminRepository
) : ViewModel() {
    private val invoiceId: String = checkNotNull(savedStateHandle["invoiceId"])
    private val _uiState = MutableStateFlow<InvoiceDetailUiState>(InvoiceDetailUiState.Loading)
    val uiState: StateFlow<InvoiceDetailUiState> = _uiState.asStateFlow()

    init {
        loadDetail()
    }

    fun loadDetail() {
        viewModelScope.launch {
            _uiState.value = InvoiceDetailUiState.Loading
            adminRepository.getInvoice(invoiceId).onSuccess { response ->
                if (!response.success) {
                    _uiState.value = InvoiceDetailUiState.Error(
                        response.message.ifBlank { "Không thể tải chi tiết hóa đơn." }
                    )
                } else {
                    _uiState.value = response.data?.let(InvoiceDetailUiState::Success)
                        ?: InvoiceDetailUiState.Empty
                }
            }.onFailure { error ->
                _uiState.value = InvoiceDetailUiState.Error(
                    error.toUserFriendlyMessage("Không thể tải chi tiết hóa đơn.")
                )
            }
        }
    }
}
