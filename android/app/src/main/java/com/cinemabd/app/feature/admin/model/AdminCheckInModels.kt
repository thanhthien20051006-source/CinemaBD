package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class CheckInRequest(
    val qrText: String
)

@Serializable
data class CheckInResultData(
    val success: Boolean = false,
    val message: String = "",
    val invoiceId: String? = null,
    val transactionRef: String? = null,
    val ticketId: String? = null,
    val seatId: String? = null,
    val customerName: String? = null,
    val movieTitle: String? = null,
    val showDate: String? = null,
    val startTime: String? = null,
    val totalAmount: Double = 0.0,
    val status: String? = null,
    val isCheckedIn: Boolean = false,
    val checkedInAt: String? = null
)
