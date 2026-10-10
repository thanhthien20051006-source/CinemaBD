package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class ReviewResponse(
    val id: Int,
    val movieId: String,
    val customerId: String,
    val content: String,
    val rating: Int,
    val isHidden: Boolean,
    val canReview: Boolean,
    val reviewRuleMessage: String? = null,
    val createdAt: String,
    val movieTitle: String? = null,
    val customerName: String? = null
)
