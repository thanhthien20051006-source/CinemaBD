package com.cinemabd.app.core.database.entity

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "tickets")
data class TicketEntity(
    @PrimaryKey
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
    val checkedInCount: Int
)
