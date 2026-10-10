package com.cinemabd.app.feature.tickets

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.utils.QrCodeGenerator
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.data.repository.TicketsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import javax.inject.Inject

@HiltViewModel
class TicketDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val ticketsRepository: TicketsRepository
) : ViewModel() {

    val invoiceId: String = checkNotNull(savedStateHandle["invoiceId"])

    private val _uiState = MutableStateFlow<TicketDetailUiState>(TicketDetailUiState.Loading)
    val uiState: StateFlow<TicketDetailUiState> = _uiState.asStateFlow()

    init {
        loadDetail()
    }

    fun loadDetail() {
        viewModelScope.launch {
            _uiState.update { TicketDetailUiState.Loading }
            val result = ticketsRepository.getInvoiceDetail(invoiceId)
            result.onSuccess { invoice ->
                val maHd = invoice.paymentId.ifBlank { invoiceId }
                val qrMap = withContext(Dispatchers.Default) {
                    if (invoice.tickets.isNotEmpty()) {
                        invoice.tickets.mapIndexed { index, ticket ->
                            val ticketCode = ticket.ticketId.ifBlank { ticket.seatId }
                            val payload = "CinemaBD|CHECKIN|${invoice.transactionRef}|$maHd|$ticketCode"
                            index to QrCodeGenerator.generateQrBitmap(payload)
                        }.toMap()
                    } else {
                        val seatTokens = invoice.seats.joinToString(",")
                        val payload = "CinemaBD|CHECKIN|${invoice.transactionRef}|$maHd|$seatTokens"
                        mapOf(0 to QrCodeGenerator.generateQrBitmap(payload))
                    }
                }
                _uiState.update {
                    TicketDetailUiState.Success(
                        invoice = invoice,
                        selectedTicketIndex = 0,
                        qrBitmaps = qrMap,
                        qrBitmap = qrMap[0]
                    )
                }
            }.onFailure { err ->
                _uiState.update { TicketDetailUiState.Error(err.toUserFriendlyMessage("Không thể tải chi tiết vé. Vui lòng thử lại.")) }
            }
        }
    }

    fun selectTicket(index: Int) {
        _uiState.update { state ->
            if (state is TicketDetailUiState.Success) {
                state.copy(selectedTicketIndex = index)
            } else {
                state
            }
        }
    }
}
