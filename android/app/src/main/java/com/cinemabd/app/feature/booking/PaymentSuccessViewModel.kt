package com.cinemabd.app.feature.booking

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.data.repository.BookingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface PaymentSuccessUiState {
    data object Loading : PaymentSuccessUiState
    data class Success(val invoice: InvoiceResponse) : PaymentSuccessUiState
    data class Error(val txnRef: String, val message: String) : PaymentSuccessUiState
}

@HiltViewModel
class PaymentSuccessViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val bookingsRepository: BookingsRepository
) : ViewModel() {

    val txnRef: String = savedStateHandle.get<String>("txnRef") ?: ""

    private val _uiState = MutableStateFlow<PaymentSuccessUiState>(PaymentSuccessUiState.Loading)
    val uiState: StateFlow<PaymentSuccessUiState> = _uiState.asStateFlow()

    init {
        loadInvoice()
    }

    fun loadInvoice() {
        if (txnRef.isBlank()) {
            _uiState.update { PaymentSuccessUiState.Error("", "Không tìm thấy mã giao dịch.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { PaymentSuccessUiState.Loading }
            val result = bookingsRepository.getInvoice(txnRef)
            result.onSuccess { invoice ->
                _uiState.update { PaymentSuccessUiState.Success(invoice) }
            }.onFailure { error ->
                _uiState.update {
                    PaymentSuccessUiState.Error(
                        txnRef = txnRef,
                        message = error.toUserFriendlyMessage("Không thể tải chi tiết hóa đơn.")
                    )
                }
            }
        }
    }
}
