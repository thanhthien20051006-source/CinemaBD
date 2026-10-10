package com.cinemabd.app.feature.tickets

import android.graphics.Bitmap
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.model.InvoiceTicketResponse

sealed interface TicketDetailUiState {
    data object Loading : TicketDetailUiState
    data class Error(val message: String? = null) : TicketDetailUiState
    data class Success(
        val invoice: InvoiceResponse,
        val selectedTicketIndex: Int = 0,
        val qrBitmaps: Map<Int, Bitmap?> = emptyMap(),
        val qrBitmap: Bitmap? = null
    ) : TicketDetailUiState {
        val currentQrBitmap: Bitmap?
            get() = qrBitmaps[selectedTicketIndex] ?: qrBitmap

        val selectedTicket: InvoiceTicketResponse?
            get() = invoice.tickets.getOrNull(selectedTicketIndex)
    }
}
