package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminRoom(
    val id: String,
    val name: String,
    val seatCount: Int = 0,
    val status: String? = null
)
