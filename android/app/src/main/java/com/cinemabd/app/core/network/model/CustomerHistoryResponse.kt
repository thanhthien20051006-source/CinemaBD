package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class CustomerHistoryResponse(
    val invoiceId: String,
    val movieTitle: String? = null,
    val showDate: String? = null,
    val startTime: String? = null,
    val totalAmount: Double = 0.0,
    val status: String? = null,
    val paymentDate: String? = null,
    val ticketCount: Int = 0,
    val checkedInCount: Int = 0,
    val seatIds: List<String> = emptyList()
)
