package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class CheckoutRequest(
    val showtimeId: String,
    val seats: List<String>,
    val combos: String? = null,
    val totalAmount: Double
)

@Serializable
data class CheckoutResponse(
    val transactionRef: String,
    val paymentUrl: String,
    val totalAmount: Double
)

@Serializable
data class PaymentCallbackResponse(
    val success: Boolean,
    val signatureValid: Boolean,
    val transactionRef: String? = null,
    val responseCode: String? = null,
    val message: String
)

@Serializable
data class InvoiceResponse(
    val transactionRef: String,
    val paymentId: String,
    val paymentStatus: String,
    val totalAmount: Double,
    val movieId: String? = null,
    val movieTitle: String? = null,
    val moviePosterUrl: String? = null,
    val showDate: String? = null,
    val startTime: String? = null,
    val roomName: String? = null,
    val seats: List<String> = emptyList(),
    val tickets: List<InvoiceTicketResponse> = emptyList(),
    val combos: List<String> = emptyList()
)

@Serializable
data class InvoiceTicketResponse(
    val ticketId: String,
    val seatId: String,
    val price: Double,
    val status: String? = null,
    val isCheckedIn: Boolean = false,
    val checkedInAt: String? = null
)
