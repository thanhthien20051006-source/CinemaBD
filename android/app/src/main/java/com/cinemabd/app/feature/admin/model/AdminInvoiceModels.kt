package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminInvoiceDetail(
    val invoiceId: String,
    val customerName: String? = null,
    val movieTitle: String? = null,
    val showDate: String? = null,
    val startTime: String? = null,
    val totalAmount: Double = 0.0,
    val status: String? = null,
    val paymentDate: String,
    val note: String? = null,
    val transactionRef: String? = null,
    val isCheckedIn: Boolean = false,
    val checkedInAt: String? = null,
    val lineItems: List<AdminInvoiceLineItem> = emptyList()
)

@Serializable
data class AdminInvoiceLineItem(
    val id: Int,
    val invoiceId: String,
    val lineType: String,
    val ticketId: String? = null,
    val comboId: String? = null,
    val itemName: String,
    val quantity: Int,
    val unitPrice: Double,
    val isCheckedIn: Boolean,
    val checkedInAt: String? = null
)
