package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.feature.admin.model.AdminRoom
import com.cinemabd.app.feature.admin.model.AdminShowtime
import java.time.LocalDate

enum class ShowtimeDisplayStatus {
    UPCOMING,   // Sắp chiếu
    ONGOING,    // Đang chiếu
    ENDED,      // Đã kết thúc
    CANCELLED   // Đã hủy
}

data class DailyStats(
    val totalTicketsSold: Int = 0,
    val checkedInTickets: Int = 0,
    val uncheckedTickets: Int = 0,
    val estimatedRevenue: Double = 0.0,
    val showtimeCount: Int = 0
) {
    val checkInRate: Float
        get() = if (totalTicketsSold > 0) {
            (checkedInTickets.toFloat() / totalTicketsSold.toFloat()).coerceIn(0f, 1f)
        } else 0f
}

data class AdminDashboardShowtimeItem(
    val raw: AdminShowtime,
    val id: String,
    val movieTitle: String,
    val roomName: String,
    val startTime: String,
    val endTime: String,
    val ticketPrice: Double,
    val totalSeats: Int,
    val soldSeats: Int,
    val availableSeats: Int,
    val checkedInSeats: Int,
    val fillRatio: Float,
    val status: ShowtimeDisplayStatus,
    val canDelete: Boolean
)

data class AdminDashboardUiState(
    val isLoading: Boolean = true,
    val isRefreshing: Boolean = false,
    val selectedDate: LocalDate = LocalDate.now(),
    val todayStats: DailyStats = DailyStats(),
    val showtimes: List<AdminDashboardShowtimeItem> = emptyList(),
    val errorMessage: String? = null,

    // Dialog state for Add Showtime
    val showAddDialog: Boolean = false,
    val availableMovies: List<MovieResponse> = emptyList(),
    val availableRooms: List<AdminRoom> = emptyList(),
    val isLoadingDialogData: Boolean = false,
    val isCreatingShowtime: Boolean = false,

    // Delete / Cancel confirmation
    val showtimeToDelete: AdminDashboardShowtimeItem? = null,
    val isDeletingShowtime: Boolean = false,

    // Action feedback (toast/snackbar)
    val actionMessage: String? = null
)
