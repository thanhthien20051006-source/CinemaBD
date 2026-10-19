package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.feature.admin.model.AdminShowtime

sealed interface AdminShowtimeListUiState {
    data class Loading(val selectedDate: String) : AdminShowtimeListUiState
    data class Error(val message: String) : AdminShowtimeListUiState
    data class Empty(val selectedDate: String) : AdminShowtimeListUiState
    data class Success(val selectedDate: String, val showtimes: List<AdminShowtime>) : AdminShowtimeListUiState
}

sealed interface AdminShowtimeDetailUiState {
    data object Loading : AdminShowtimeDetailUiState
    data object Empty : AdminShowtimeDetailUiState
    data class Error(val message: String) : AdminShowtimeDetailUiState
    data class Success(val showtime: AdminShowtime) : AdminShowtimeDetailUiState
}

sealed interface AdminMovieListUiState {
    data object Loading : AdminMovieListUiState
    data class Error(val message: String) : AdminMovieListUiState
    data class Empty(val query: String) : AdminMovieListUiState
    data class Success(val query: String, val movies: List<MovieResponse>) : AdminMovieListUiState
}

sealed interface AdminMovieDetailUiState {
    data object Loading : AdminMovieDetailUiState
    data object Empty : AdminMovieDetailUiState
    data class Error(val message: String) : AdminMovieDetailUiState
    data class Success(val movie: MovieResponse, val isSaving: Boolean = false) : AdminMovieDetailUiState
}
