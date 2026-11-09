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
                val signedTicketsResult = ticketsRepository.getMyTicketPayloads()
                val signedTickets = signedTicketsResult.getOrNull()
                    ?.filter { it.transactionRef == invoice.transactionRef }
                    ?.associateBy { it.ticketId }
                    .orEmpty()
                val qrMap = withContext(Dispatchers.Default) {
                    invoice.tickets.mapIndexedNotNull { index, ticket ->
                        val signedTicket = signedTickets[ticket.ticketId] ?: return@mapIndexedNotNull null
                        index to QrCodeGenerator.generateQrBitmap(signedTicket.qrPayload)
                    }.toMap()
                }
                _uiState.update {
                    TicketDetailUiState.Success(
                        invoice = invoice,
                        selectedTicketIndex = qrMap.keys.firstOrNull() ?: 0,
                        qrBitmaps = qrMap
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
