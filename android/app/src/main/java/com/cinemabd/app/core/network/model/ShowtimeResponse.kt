package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class ShowtimeResponse(
    val id: String,
    val showDate: String,
    val startTime: String,
    val roomId: String,
    val roomName: String,
    val ticketPrice: Double,
    val totalSeats: Int,
    val availableSeats: Int,
    val status: String? = null,
    val heldSeats: Int = 0,
    val soldSeats: Int = 0,
    val checkedInSeats: Int = 0,
    val canEdit: Boolean = true,
    val canDelete: Boolean = true
)
