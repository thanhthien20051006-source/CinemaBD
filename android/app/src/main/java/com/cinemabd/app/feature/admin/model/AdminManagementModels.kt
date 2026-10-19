package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminShowtime(
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
    val canDelete: Boolean = true,
    val movieId: String? = null,
    val movieTitle: String? = null
)

@Serializable
data class AdminMovieUpsertRequest(
    val title: String,
    val genre: String? = null,
    val durationMinutes: Int,
    val director: String? = null,
    val cast: String? = null,
    val country: String? = null,
    val ageRestriction: Int? = null,
    val description: String? = null,
    val posterUrl: String? = null,
    val trailerUrl: String? = null,
    val releaseDate: String? = null,
    val endDate: String? = null,
    val status: String? = null
)
