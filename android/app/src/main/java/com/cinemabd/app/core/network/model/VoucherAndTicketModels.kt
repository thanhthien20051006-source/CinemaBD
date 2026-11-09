package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class VoucherPreviewRequest(
    val showtimeId: String,
    val seats: List<String>,
    val combos: String? = null,
    val code: String
)

@Serializable
data class VoucherPreviewResponse(
    val success: Boolean,
    val message: String,
    val code: String? = null,
    val subtotal: Double = 0.0,
    val discountAmount: Double = 0.0,
    val finalAmount: Double = 0.0
)

@Serializable
data class CustomerTicketResponse(
    val ticketId: String,
    val transactionRef: String,
    val movieTitle: String,
    val moviePosterUrl: String,
    val showDate: String,
    val startTime: String,
    val roomName: String,
    val seatId: String,
    val price: Double,
    val status: String,
    val checkedInAt: String? = null,
    val qrPayload: String,
    val qrSignature: String,
    val qrIssuedAtUtc: String
)
