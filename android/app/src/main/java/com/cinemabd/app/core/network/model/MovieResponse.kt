package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class MovieResponse(
    val id: String,
    val title: String,
    val genre: String? = null,
    val durationMinutes: Int = 0,
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
