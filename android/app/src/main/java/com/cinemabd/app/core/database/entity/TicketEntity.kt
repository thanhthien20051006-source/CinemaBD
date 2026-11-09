package com.cinemabd.app.core.database.entity

import androidx.room.Entity

@Entity(
    tableName = "tickets",
    primaryKeys = ["userId", "invoiceId"]
)
data class TicketEntity(
    val userId: String,
    val invoiceId: String,
    val movieTitle: String?,
    val showDate: String?,
    val startTime: String?,
    val roomName: String?,
    val seats: String,
    val status: String?,
    val totalAmount: Double,
    val paymentDate: String?,
    val ticketCount: Int,
    val checkedInCount: Int,
    val qrPayload: String? = null
)
