package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class ComboResponse(
    val id: String,
    val name: String,
    val price: Double,
    val description: String? = null,
    val imageUrl: String? = null
)
