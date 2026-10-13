package com.cinemabd.app.feature.admin

sealed interface CheckInResultState {
    data class Success(
        val ticketId: String?,
        val movieTitle: String?,
        val seatId: String?,
        val customerName: String?,
        val message: String
    ) : CheckInResultState

    data class AlreadyCheckedIn(
        val ticketId: String?,
        val movieTitle: String?,
        val seatId: String?,
        val message: String
    ) : CheckInResultState

    data class InvalidQr(
        val message: String
    ) : CheckInResultState

    data class NotFound(
        val message: String
    ) : CheckInResultState

    data class Error(
        val message: String
    ) : CheckInResultState
}

data class CheckInUiState(
    val qrInput: String = "",
    val isLoading: Boolean = false,
    val resultState: CheckInResultState? = null,
    val cameraPermissionDenied: Boolean = false
)
