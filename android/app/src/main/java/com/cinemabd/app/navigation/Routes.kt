package com.cinemabd.app.navigation

object Routes {
    // Root Nav
    const val AUTH = "auth"
    const val MAIN = "main"

    // Main Nav
    const val HOME = "home"
    const val TICKETS = "tickets"
    const val PROFILE = "profile"
    
    // Feature Nav
    const val MOVIE_DETAIL = "movie_detail/{movieId}"
    fun movieDetail(movieId: String) = "movie_detail/$movieId"
    
    const val SELECT_SHOWTIMES = "select_showtimes/{movieId}"
    fun selectShowtimes(movieId: String) = "select_showtimes/$movieId"

    const val SELECT_SEATS = "select_seats/{showtimeId}"
    fun selectSeats(showtimeId: String) = "select_seats/$showtimeId"
}
