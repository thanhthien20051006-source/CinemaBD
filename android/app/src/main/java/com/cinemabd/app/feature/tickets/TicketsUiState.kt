package com.cinemabd.app.feature.tickets

data class TicketItemUiModel(
    val invoiceId: String,
    val movieTitle: String,
    val showDate: String?,
    val startTime: String?,
    val showDateTimeText: String,
    val roomName: String?,
    val seatsText: String,
    val status: String?,
    val totalAmount: Double,
    val totalAmountText: String,
    val isUpcoming: Boolean
)

sealed interface TicketsUiState {
    data object Loading : TicketsUiState
    data class Error(val message: String? = null) : TicketsUiState
    data object Empty : TicketsUiState
    data class Success(
        val upcomingTickets: List<TicketItemUiModel>,
        val pastTickets: List<TicketItemUiModel>,
        val isOffline: Boolean = false
    ) : TicketsUiState
}
