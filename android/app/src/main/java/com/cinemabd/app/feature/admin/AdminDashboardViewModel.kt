package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminShowtime
import com.cinemabd.app.feature.admin.model.AdminShowtimeUpsertRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import javax.inject.Inject

@HiltViewModel
class AdminDashboardViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(AdminDashboardUiState())
    val uiState: StateFlow<AdminDashboardUiState> = _uiState.asStateFlow()

    init {
        loadDashboardData(LocalDate.now())
    }

    fun loadDashboardData(
        date: LocalDate = _uiState.value.selectedDate,
        isRefresh: Boolean = false,
        isSilent: Boolean = false
    ) {
        viewModelScope.launch {
            if (!isSilent) {
                if (isRefresh) {
                    _uiState.update { it.copy(isRefreshing = true, errorMessage = null) }
                } else {
                    _uiState.update { it.copy(isLoading = true, selectedDate = date, errorMessage = null) }
                }
            }

            try {
                coroutineScope {
                    val dateStr = date.format(DateTimeFormatter.ISO_LOCAL_DATE)
                    val showtimesDeferred = async { adminRepository.getShowtimes(date = dateStr) }
                    val moviesDeferred = async { adminRepository.getMovies(null) }

                    val showtimesResult = showtimesDeferred.await()
                    val moviesResult = moviesDeferred.await()

                    val movieMap = moviesResult.getOrNull()?.data?.associateBy { it.id }.orEmpty()

                    showtimesResult.onSuccess { response ->
                        if (!response.success || response.data == null) {
                            if (!isSilent) {
                                _uiState.update {
                                    it.copy(
                                        isLoading = false,
                                        isRefreshing = false,
                                        errorMessage = response.message.ifBlank { "Không thể tải danh sách suất chiếu." }
                                    )
                                }
                            }
                        } else {
                            val rawList = response.data
                            val mappedItems = rawList.map { showtime ->
                                val durationMinutes = movieMap[showtime.movieId]?.durationMinutes ?: 120
                                val (startFormatted, endFormatted) = parseStartAndEndTime(
                                    showDate = showtime.showDate,
                                    startTime = showtime.startTime,
                                    durationMinutes = durationMinutes
                                )
                                val status = computeShowtimeStatus(
                                    showDate = showtime.showDate,
                                    startTime = showtime.startTime,
                                    durationMinutes = durationMinutes,
                                    backendStatus = showtime.status
                                )
                                val fillRatio = if (showtime.totalSeats > 0) {
                                    (showtime.soldSeats.toFloat() / showtime.totalSeats.toFloat()).coerceIn(0f, 1f)
                                } else 0f

                                val movieTitle = showtime.movieTitle
                                    ?: movieMap[showtime.movieId]?.title
                                    ?: showtime.movieId.orEmpty().ifBlank { "Suất chiếu ${showtime.id}" }

                                AdminDashboardShowtimeItem(
                                    raw = showtime,
                                    id = showtime.id,
                                    movieTitle = movieTitle,
                                    roomName = showtime.roomName.ifBlank { "Phòng ${showtime.roomId}" },
                                    startTime = startFormatted,
                                    endTime = endFormatted,
                                    ticketPrice = showtime.ticketPrice,
                                    totalSeats = showtime.totalSeats,
                                    soldSeats = showtime.soldSeats,
                                    availableSeats = showtime.availableSeats,
                                    checkedInSeats = showtime.checkedInSeats,
                                    fillRatio = fillRatio,
                                    status = status,
                                    canDelete = showtime.canDelete && showtime.soldSeats == 0
                                )
                            }

                            val totalSold = rawList.sumOf { it.soldSeats }
                            val checkedIn = rawList.sumOf { it.checkedInSeats }
                            val unchecked = (totalSold - checkedIn).coerceAtLeast(0)
                            val estimatedRevenue = rawList.sumOf { it.ticketPrice * it.soldSeats }

                            val stats = DailyStats(
                                totalTicketsSold = totalSold,
                                checkedInTickets = checkedIn,
                                uncheckedTickets = unchecked,
                                estimatedRevenue = estimatedRevenue,
                                showtimeCount = rawList.size
                            )

                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    isRefreshing = false,
                                    selectedDate = date,
                                    todayStats = stats,
                                    showtimes = mappedItems,
                                    errorMessage = null
                                )
                            }
                        }
                    }.onFailure { error ->
                        if (!isSilent) {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    isRefreshing = false,
                                    errorMessage = error.toUserFriendlyMessage("Không thể tải danh sách suất chiếu.")
                                )
                            }
                        }
                    }
                }
            } catch (e: Exception) {
                if (!isSilent) {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isRefreshing = false,
                            errorMessage = e.toUserFriendlyMessage("Lỗi kết nối khi tải dữ liệu dashboard.")
                        )
                    }
                }
            }
        }
    }

    fun onDateSelected(newDate: LocalDate) {
        if (_uiState.value.selectedDate == newDate && !_uiState.value.isLoading) return
        loadDashboardData(newDate)
    }

    fun onRefresh() {
        loadDashboardData(_uiState.value.selectedDate, isRefresh = true, isSilent = false)
    }

    fun refresh(silent: Boolean = true) {
        loadDashboardData(_uiState.value.selectedDate, isRefresh = !silent, isSilent = silent)
    }

    fun silentRefresh() {
        refresh(silent = true)
    }

    fun onDeleteShowtimeClick(item: AdminDashboardShowtimeItem) {
        if (item.soldSeats > 0) {
            _uiState.update {
                it.copy(actionMessage = "Không thể hủy/xóa suất chiếu đã có vé được đặt.")
            }
            return
        }
        _uiState.update { it.copy(showtimeToDelete = item) }
    }

    fun dismissDeleteDialog() {
        _uiState.update { it.copy(showtimeToDelete = null) }
    }

    fun confirmDeleteShowtime() {
        val target = _uiState.value.showtimeToDelete ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isDeletingShowtime = true) }
            adminRepository.deleteShowtime(target.id).onSuccess { response ->
                if (response.success) {
                    _uiState.update {
                        it.copy(
                            showtimeToDelete = null,
                            isDeletingShowtime = false,
                            actionMessage = "Đã xóa suất chiếu thành công."
                        )
                    }
                    loadDashboardData(_uiState.value.selectedDate, isRefresh = true)
                } else {
                    _uiState.update {
                        it.copy(
                            isDeletingShowtime = false,
                            actionMessage = response.message.ifBlank { "Không thể xóa suất chiếu." }
                        )
                    }
                }
            }.onFailure { error ->
                _uiState.update {
                    it.copy(
                        isDeletingShowtime = false,
                        actionMessage = error.toUserFriendlyMessage("Lỗi khi xóa suất chiếu.")
                    )
                }
            }
        }
    }

    fun openAddShowtimeDialog() {
        _uiState.update { it.copy(showAddDialog = true) }
    }

    fun dismissAddShowtimeDialog() {
        _uiState.update { it.copy(showAddDialog = false) }
    }

    fun clearActionMessage() {
        _uiState.update { it.copy(actionMessage = null) }
    }

    private fun parseStartAndEndTime(
        showDate: String,
        startTime: String,
        durationMinutes: Int
    ): Pair<String, String> {
        val cleanTime = startTime.substringBefore('.').trim()
        val parsedTime = try {
            when (cleanTime.count { it == ':' }) {
                2 -> LocalTime.parse(cleanTime, DateTimeFormatter.ofPattern("HH:mm:ss"))
                1 -> LocalTime.parse(cleanTime, DateTimeFormatter.ofPattern("HH:mm"))
                else -> null
            }
        } catch (_: Exception) {
            null
        }

        val startFormatted = parsedTime?.format(DateTimeFormatter.ofPattern("HH:mm"))
            ?: cleanTime.take(5)
        val endFormatted = parsedTime?.plusMinutes(durationMinutes.toLong())
            ?.format(DateTimeFormatter.ofPattern("HH:mm")) ?: "—"

        return Pair(startFormatted, endFormatted)
    }

    private fun computeShowtimeStatus(
        showDate: String,
        startTime: String,
        durationMinutes: Int,
        backendStatus: String?
    ): ShowtimeDisplayStatus {
        if (backendStatus.equals("Cancelled", ignoreCase = true)) {
            return ShowtimeDisplayStatus.CANCELLED
        }

        val date = try {
            LocalDate.parse(showDate.substringBefore('T').trim())
        } catch (_: Exception) {
            null
        } ?: return ShowtimeDisplayStatus.UPCOMING

        val cleanTime = startTime.substringBefore('.').trim()
        val time = try {
            when (cleanTime.count { it == ':' }) {
                2 -> LocalTime.parse(cleanTime, DateTimeFormatter.ofPattern("HH:mm:ss"))
                1 -> LocalTime.parse(cleanTime, DateTimeFormatter.ofPattern("HH:mm"))
                else -> null
            }
        } catch (_: Exception) {
            null
        } ?: return ShowtimeDisplayStatus.UPCOMING

        val startDateTime = LocalDateTime.of(date, time)
        val endDateTime = startDateTime.plusMinutes(durationMinutes.toLong())
        val now = LocalDateTime.now()

        return when {
            now.isBefore(startDateTime) -> ShowtimeDisplayStatus.UPCOMING
            now.isAfter(endDateTime) -> ShowtimeDisplayStatus.ENDED
            else -> ShowtimeDisplayStatus.ONGOING
        }
    }
}
