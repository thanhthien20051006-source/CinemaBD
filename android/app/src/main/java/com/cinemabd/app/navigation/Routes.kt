package com.cinemabd.app.navigation

object Routes {
    // Root Nav
    const val AUTH = "auth"
    const val MAIN = "main"

    // Main Nav
    const val HOME = "home"
    const val TICKETS = "tickets"
    const val PROFILE = "profile"
    const val CHECK_IN = "check_in"
    
    // Feature Nav
    const val MOVIE_DETAIL = "movie_detail/{movieId}"
    fun movieDetail(movieId: String) = "movie_detail/$movieId"
    
    const val SELECT_SHOWTIMES = "select_showtimes/{movieId}"
    fun selectShowtimes(movieId: String) = "select_showtimes/$movieId"

    // Booking Flow
    const val BOOKING_GRAPH = "booking_flow"

    const val SELECT_SEATS = "select_seats/{showtimeId}"
    fun selectSeats(showtimeId: String) = "select_seats/$showtimeId"

    const val SELECT_COMBO = "select_combo/{showtimeId}"
    fun selectCombo(showtimeId: String) = "select_combo/$showtimeId"

    const val CHECKOUT = "checkout/{showtimeId}"
    fun checkout(showtimeId: String) = "checkout/$showtimeId"

    const val PAYMENT_SUCCESS = "payment_success/{txnRef}"
    fun paymentSuccess(txnRef: String) = "payment_success/$txnRef"

    const val TICKET_DETAIL = "ticket_detail/{invoiceId}"
    fun ticketDetail(invoiceId: String) = "ticket_detail/$invoiceId"
}
