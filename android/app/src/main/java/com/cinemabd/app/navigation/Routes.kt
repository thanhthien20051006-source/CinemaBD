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
    const val ADMIN_INVOICES = "admin_invoices"
    const val ADMIN_STATISTICS = "admin_statistics"
    const val ADMIN_MANAGEMENT = "admin_management"
    const val ADMIN_SHOWTIMES = "admin_showtimes"
    const val ADMIN_SHOWTIME_DETAIL = "admin_showtime_detail/{showtimeId}/{showDate}"
    const val ADMIN_MOVIES = "admin_movies"
    const val ADMIN_MOVIE_DETAIL = "admin_movie_detail/{movieId}"
    const val ADMIN_INVOICE_DETAIL = "admin_invoice_detail/{invoiceId}"
    fun adminInvoiceDetail(invoiceId: String) = "admin_invoice_detail/$invoiceId"
    fun adminShowtimeDetail(showtimeId: String, showDate: String) = "admin_showtime_detail/$showtimeId/$showDate"
    fun adminMovieDetail(movieId: String) = "admin_movie_detail/$movieId"

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
