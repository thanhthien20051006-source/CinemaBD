package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminRoom
import com.cinemabd.app.feature.admin.model.AdminShowtime
import com.cinemabd.app.feature.admin.model.AdminShowtimeUpsertRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.async
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

typealias MovieDto = MovieResponse
typealias CinemaHallDto = AdminRoom
typealias ShowtimeDto = AdminShowtime

data class AddShowtimeUiState(
    val isLoadingData: Boolean = false,
    val movies: List<MovieDto> = emptyList(),
    val selectedMovie: MovieDto? = null,
    val rooms: List<CinemaHallDto> = emptyList(),
    val selectedRoom: CinemaHallDto? = null,
    val selectedDate: LocalDate = LocalDate.now(),
    val selectedTime: LocalTime = LocalTime.of(
        LocalTime.now().plusHours(1).hour.coerceIn(0, 23),
        0
    ),
    val ticketPrice: String = "75000",
    val existingShowtimes: List<ShowtimeDto> = emptyList(),
    val conflictError: String? = null,
    val validationError: String? = null,
    val isSubmitting: Boolean = false,
    val submitSuccess: Boolean = false,
    val generalError: String? = null
) {
    val durationMinutes: Int
        get() = selectedMovie?.durationMinutes?.coerceAtLeast(1) ?: 120

    val expectedEndTime: LocalTime
        get() = selectedTime.plusMinutes(durationMinutes.toLong() + 15L)

    val isPriceValid: Boolean
        get() = (ticketPrice.toDoubleOrNull() ?: 0.0) >= 40000.0

    val isTimeInPast: Boolean
        get() = LocalDateTime.of(selectedDate, selectedTime).isBefore(LocalDateTime.now())

    val isFormValid: Boolean
        get() = selectedMovie != null &&
                selectedRoom != null &&
                isPriceValid &&
                !isTimeInPast &&
                conflictError == null &&
                !isSubmitting
}

@HiltViewModel
class AddShowtimeViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(AddShowtimeUiState())
    val uiState: StateFlow<AddShowtimeUiState> = _uiState.asStateFlow()

    private var initializedForDate: LocalDate? = null

    fun init(initialDate: LocalDate, forceRefresh: Boolean = false) {
        if (!forceRefresh && initializedForDate == initialDate && _uiState.value.movies.isNotEmpty()) {
            return
        }
        initializedForDate = initialDate

        val defaultTime = LocalTime.of(
            LocalTime.now().plusHours(1).hour.coerceIn(0, 23),
            0
        )

        _uiState.update {
            it.copy(
                selectedDate = initialDate,
                selectedTime = defaultTime,
                ticketPrice = "75000",
                conflictError = null,
                validationError = null,
                generalError = null,
                submitSuccess = false,
                isSubmitting = false
            )
        }
        loadInitialData(initialDate)
    }

    private fun loadInitialData(date: LocalDate) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoadingData = true, generalError = null) }

            val dateStr = date.format(DateTimeFormatter.ISO_LOCAL_DATE)
            val moviesDeferred = async { adminRepository.getMovies(null) }
            val roomsDeferred = async { adminRepository.getRooms() }
            val showtimesDeferred = async { adminRepository.getShowtimes(date = dateStr) }

            val moviesResult = moviesDeferred.await()
            val roomsResult = roomsDeferred.await()
            val showtimesResult = showtimesDeferred.await()

            val movies = moviesResult.getOrNull()?.data.orEmpty()
            val rooms = roomsResult.getOrNull()?.data.orEmpty().filter { room ->
                room.status == null ||
                        room.status.equals("Active", ignoreCase = true) ||
                        room.status.contains("Hoạt động", ignoreCase = true) ||
                        room.status.contains("Hoat dong", ignoreCase = true)
            }
            val showtimes = showtimesResult.getOrNull()?.data.orEmpty()

            _uiState.update { state ->
                val selectedMovie = state.selectedMovie ?: movies.firstOrNull()
                val selectedRoom = state.selectedRoom ?: rooms.firstOrNull()
                state.copy(
                    isLoadingData = false,
                    movies = movies,
                    selectedMovie = selectedMovie,
                    rooms = rooms,
                    selectedRoom = selectedRoom,
                    existingShowtimes = showtimes
                )
            }
            validateScheduleConflict()
        }
    }

    fun onMovieSelected(movie: MovieDto) {
        _uiState.update { it.copy(selectedMovie = movie) }
        validateScheduleConflict()
    }

    fun onRoomSelected(room: CinemaHallDto) {
        _uiState.update { it.copy(selectedRoom = room) }
        validateScheduleConflict()
    }

    fun onDateSelected(newDate: LocalDate) {
        if (_uiState.value.selectedDate == newDate) return
        _uiState.update { it.copy(selectedDate = newDate) }
        loadShowtimesForDate(newDate)
    }

    private fun loadShowtimesForDate(date: LocalDate) {
        viewModelScope.launch {
            val dateStr = date.format(DateTimeFormatter.ISO_LOCAL_DATE)
            adminRepository.getShowtimes(date = dateStr).onSuccess { response ->
                val showtimes = response.data.orEmpty()
                _uiState.update { it.copy(existingShowtimes = showtimes) }
                validateScheduleConflict()
            }.onFailure {
                validateScheduleConflict()
            }
        }
    }

    fun onTimeSelected(time: LocalTime) {
        _uiState.update { it.copy(selectedTime = time) }
        validateScheduleConflict()
    }

    fun onTicketPriceChanged(priceText: String) {
        val filtered = priceText.filter { it.isDigit() }
        _uiState.update { it.copy(ticketPrice = filtered) }
        validateScheduleConflict()
    }

    fun validateScheduleConflict() {
        val state = _uiState.value
        val movie = state.selectedMovie
        val room = state.selectedRoom
        val date = state.selectedDate
        val startTime = state.selectedTime

        if (movie == null || room == null) {
            _uiState.update { it.copy(conflictError = null) }
            validateFormFields()
            return
        }

        val durationN = movie.durationMinutes.coerceAtLeast(1)
        val startDateTimeN = LocalDateTime.of(date, startTime)
        val endDateTimeN = startDateTimeN.plusMinutes(durationN.toLong() + 15L)

        val movieMap = state.movies.associateBy { it.id }

        // Filter existing showtimes in the same room on this date (exclude cancelled)
        val roomShowtimes = state.existingShowtimes.filter {
            it.roomId == room.id && !it.status.equals("Cancelled", ignoreCase = true)
        }

        var conflictMessage: String? = null

        for (existing in roomShowtimes) {
            val parsedStart = parseLocalTime(existing.startTime) ?: continue
            val existingMovie = movieMap[existing.movieId]
            val durationE = existingMovie?.durationMinutes?.coerceAtLeast(1) ?: 120

            val startDateTimeE = LocalDateTime.of(date, parsedStart)
            val endDateTimeE = startDateTimeE.plusMinutes(durationE.toLong() + 15L)

            // Overlap condition: max(Start_N, Start_E) < min(End_N, End_E)
            val maxStart = if (startDateTimeN.isAfter(startDateTimeE)) startDateTimeN else startDateTimeE
            val minEnd = if (endDateTimeN.isBefore(endDateTimeE)) endDateTimeN else endDateTimeE

            if (maxStart.isBefore(minEnd)) {
                val existingTitle = existing.movieTitle
                    ?: existingMovie?.title
                    ?: existing.movieId.orEmpty().ifBlank { "Khác" }
                val startFormatted = parsedStart.format(DateTimeFormatter.ofPattern("HH:mm"))
                val endFormatted = endDateTimeE.toLocalTime().format(DateTimeFormatter.ofPattern("HH:mm"))
                conflictMessage = "⚠️ Trùng lịch với suất chiếu $existingTitle từ $startFormatted đến $endFormatted"
                break
            }
        }

        _uiState.update { it.copy(conflictError = conflictMessage) }
        validateFormFields()
    }

    private fun validateFormFields() {
        val state = _uiState.value
        val error = when {
            state.selectedMovie == null -> "Vui lòng chọn phim"
            state.selectedRoom == null -> "Vui lòng chọn phòng chiếu"
            state.isTimeInPast -> "Giờ bắt đầu không được nằm trong quá khứ so với thời điểm hiện tại"
            !state.isPriceValid -> "Giá vé tiêu chuẩn phải từ 40.000 VND trở lên"
            else -> null
        }
        _uiState.update { it.copy(validationError = error) }
    }

    fun submitShowtime(onSuccess: () -> Unit) {
        val state = _uiState.value
        if (!state.isFormValid) return

        val movie = state.selectedMovie ?: return
        val room = state.selectedRoom ?: return
        val price = state.ticketPrice.toDoubleOrNull() ?: return

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, generalError = null) }

            val timeFormatted = state.selectedTime.format(DateTimeFormatter.ofPattern("HH:mm:ss"))
            val request = AdminShowtimeUpsertRequest(
                movieId = movie.id,
                roomId = room.id,
                showDate = state.selectedDate.format(DateTimeFormatter.ISO_LOCAL_DATE),
                startTime = timeFormatted,
                ticketPrice = price
            )

            adminRepository.createShowtime(request).onSuccess { response ->
                if (response.success) {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            submitSuccess = true,
                            generalError = null
                        )
                    }
                    onSuccess()
                } else {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            generalError = response.message.ifBlank { "Không thể tạo suất chiếu." }
                        )
                    }
                }
            }.onFailure { error ->
                val errorMessage = error.toUserFriendlyMessage("Lỗi khi tạo suất chiếu.")
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        generalError = errorMessage
                    )
                }
            }
        }
    }

    private fun parseLocalTime(raw: String): LocalTime? {
        val clean = raw.substringBefore('.').trim()
        return try {
            when (clean.count { it == ':' }) {
                2 -> LocalTime.parse(clean, DateTimeFormatter.ofPattern("HH:mm:ss"))
                1 -> LocalTime.parse(clean, DateTimeFormatter.ofPattern("HH:mm"))
                else -> null
            }
        } catch (_: Exception) {
            null
        }
    }
}
