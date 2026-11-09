package com.cinemabd.app.feature.admin.scanner

import com.cinemabd.app.feature.admin.model.CheckInResultData

data class ScanTicketUiState(
    val isCameraPermissionGranted: Boolean = false,
    val isTorchEnabled: Boolean = false,
    val scanStatus: ScanStatus = ScanStatus.Idle
)

sealed interface ScanStatus {
    data object Idle : ScanStatus
    data object Verifying : ScanStatus

    data class Valid(
        val data: CheckInResultData,
        val message: String
    ) : ScanStatus

    data class AlreadyUsed(
        val data: CheckInResultData?,
        val message: String
    ) : ScanStatus

    data class InvalidOrTampered(
        val message: String
    ) : ScanStatus

    data class NotFound(
        val message: String
    ) : ScanStatus

    data class Error(
        val message: String
    ) : ScanStatus
}
