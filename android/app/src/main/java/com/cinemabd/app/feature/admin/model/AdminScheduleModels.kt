package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminShowtimeUpsertRequest(
    val movieId: String,
    val roomId: String,
    val showDate: String,
    val startTime: String,
    val ticketPrice: Double,
    val status: String? = null
)

@Serializable
data class AdminShowtimeGenerateRequest(
    val movieIds: List<String>,
    val roomIds: List<String>,
    val fromDate: String,
    val toDate: String,
    val startTimes: List<String>,
    val ticketPrice: Double
)

@Serializable
data class AdminShowtimeGenerateResult(
    val created: Int = 0,
    val skipped: Int = 0,
    val messages: List<String> = emptyList()
)

@Serializable
data class ExpirePassedShowtimesResult(
    val expired: Int = 0
)
