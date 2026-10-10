package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class SeatResponse(
    val id: String,
    val row: String,
    val column: String,
    val seatType: String? = null,
    val isBooked: Boolean,
    val status: String,
    val price: Double
)

@Serializable
data class SeatHoldRequest(
    val showtimeId: String,
    val seats: List<String>
)

@Serializable
data class SeatHoldResult(
    val success: Boolean,
    val message: String = "",
    val showtimeId: String = "",
    val heldSeats: List<String> = emptyList(),
    val unavailableSeats: List<String> = emptyList(),
    val expiresAt: String? = null
)
