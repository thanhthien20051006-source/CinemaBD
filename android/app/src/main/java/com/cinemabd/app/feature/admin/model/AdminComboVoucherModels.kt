package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminCombo(
    val id: String,
    val name: String,
    val price: Double = 0.0,
    val description: String? = null,
    val imageUrl: String? = null
)

@Serializable
data class AdminComboRequest(
    val id: String,
    val name: String,
    val price: Double,
    val description: String? = null,
    val imageUrl: String? = null
)

@Serializable
data class AdminVoucher(
    val id: String,
    val customerId: String = "ALL",
    val customerName: String? = null,
    val isGlobal: Boolean = false,
    val code: String,
    val description: String? = null,
    val expiredAt: String,
    val discountValue: Double = 0.0,
    val discountType: String = "Amount",
    val minOrderAmount: Double = 0.0,
    val maxDiscountAmount: Double = 0.0,
    val isUsed: Boolean = false,
    val usedAt: String? = null,
    val usedTransactionRef: String? = null,
    val isExpired: Boolean = false,
    val isActive: Boolean = true
)

@Serializable
data class AdminVoucherRequest(
    val id: String,
    val customerId: String = "ALL",
    val isGlobal: Boolean = true,
    val code: String,
    val description: String? = null,
    val expiredAt: String,
    val discountValue: Double,
    val discountType: String = "Amount",
    val minOrderAmount: Double = 0.0,
    val maxDiscountAmount: Double = 0.0
)

@Serializable
data class AdminRefundRequest(
    val id: Int,
    val invoiceId: String,
    val ticketId: String? = null,
    val customerId: String,
    val customerName: String? = null,
    val movieTitle: String? = null,
    val showDate: String? = null,
    val startTime: String? = null,
    val reason: String = "",
    val status: String = "Pending",
    val requestedAt: String = "",
    val approvedAt: String? = null,
    val rejectedAt: String? = null,
    val refundAmount: Double = 0.0,
    val adminNote: String? = null
)

@Serializable
data class AdminRefundActionRequest(
    val adminNote: String? = null
)
