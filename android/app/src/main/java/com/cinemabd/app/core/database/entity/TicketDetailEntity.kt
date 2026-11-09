package com.cinemabd.app.core.database.entity

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(
    tableName = "ticket_details",
    primaryKeys = ["userId", "ticketId"],
    indices = [Index(value = ["userId", "invoiceId"])]
)
data class TicketDetailEntity(
    val ticketId: String,
    val userId: String,
    val invoiceId: String,
    val movieTitle: String?,
    val roomName: String?,
    val showDate: String?,
    val startTime: String?,
    val seatId: String,
    val price: Double,
    val status: String?,
    val isCheckedIn: Boolean,
    val checkedInAt: String?,
    val qrPayload: String,
    val qrSignature: String,
    val qrIssuedAtUtc: String?
)
