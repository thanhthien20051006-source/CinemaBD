package com.cinemabd.app.feature.tickets

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.data.repository.TicketsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class TicketsViewModel @Inject constructor(
    private val ticketsRepository: TicketsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<TicketsUiState>(TicketsUiState.Loading)
    val uiState: StateFlow<TicketsUiState> = _uiState.asStateFlow()

    init {
        fetchTickets()
    }

    fun fetchTickets() {
        viewModelScope.launch {
            _uiState.update { TicketsUiState.Loading }
            loadTicketsInternal()
        }
    }

    fun refreshTickets() {
        viewModelScope.launch {
            loadTicketsInternal()
        }
    }

    private suspend fun loadTicketsInternal() {
        val result = ticketsRepository.getTickets()
        result.onSuccess { data ->
            if (data.tickets.isEmpty()) {
                _uiState.update { TicketsUiState.Empty }
            } else {
                val uiModels = data.tickets.map { dto ->
                    val isUpcoming = DateTimeUtils.isUpcoming(dto.showDate, dto.startTime)
                    val showDateTimeText = DateTimeUtils.formatShowDateTime(dto.showDate, dto.startTime)
                    val room = data.roomMap[dto.invoiceId]
                    val seatsText = dto.seatIds.joinToString(", ").ifBlank { "—" }
                    val totalAmountText = DateTimeUtils.formatCurrency(dto.totalAmount)

                    TicketItemUiModel(
                        invoiceId = dto.invoiceId,
                        movieTitle = dto.movieTitle ?: "—",
                        showDate = dto.showDate,
                        startTime = dto.startTime,
                        showDateTimeText = showDateTimeText,
                        roomName = room,
                        seatsText = seatsText,
                        status = dto.status,
                        totalAmount = dto.totalAmount,
                        totalAmountText = totalAmountText,
                        isUpcoming = isUpcoming
                    )
                }

                val upcoming = uiModels.filter { it.isUpcoming }
                val past = uiModels.filterNot { it.isUpcoming }

                _uiState.update {
                    TicketsUiState.Success(
                        upcomingTickets = upcoming,
                        pastTickets = past,
                        isOffline = data.isOffline
                    )
                }
            }
        }.onFailure { err ->
            _uiState.update { TicketsUiState.Error(err.message) }
        }
    }
}
