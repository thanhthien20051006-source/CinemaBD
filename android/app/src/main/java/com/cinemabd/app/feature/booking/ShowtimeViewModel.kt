package com.cinemabd.app.feature.booking

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.ShowtimeResponse
import com.cinemabd.app.data.repository.MoviesRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import javax.inject.Inject

data class DateItem(
    val date: LocalDate,
    val label: String,
    val isSelected: Boolean = false
)

data class RoomShowtimes(
    val roomName: String,
    val showtimes: List<ShowtimeResponse>
)

sealed interface ShowtimeUiState {
    data object Loading : ShowtimeUiState
    data class Success(
        val dates: List<DateItem>,
        val selectedDate: LocalDate,
        val roomShowtimes: List<RoomShowtimes>
    ) : ShowtimeUiState
    data class Error(val message: String) : ShowtimeUiState
    data object Empty : ShowtimeUiState
}

@HiltViewModel
class ShowtimeViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val moviesRepository: MoviesRepository
) : ViewModel() {

    private val movieId: String = checkNotNull(savedStateHandle["movieId"])

    private val _uiState = MutableStateFlow<ShowtimeUiState>(ShowtimeUiState.Loading)
    val uiState: StateFlow<ShowtimeUiState> = _uiState.asStateFlow()

    private val apiDateFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd")

    init {
        loadShowtimes(null)
    }

    fun selectDate(date: LocalDate) {
        loadShowtimes(date)
    }

    fun retry() {
        val currentDate = (_uiState.value as? ShowtimeUiState.Success)?.selectedDate
        loadShowtimes(currentDate)
    }

    private fun loadShowtimes(date: LocalDate?) {
        viewModelScope.launch {
            _uiState.update { ShowtimeUiState.Loading }

            val dateStr = date?.format(apiDateFormatter)
            val result = moviesRepository.getShowtimes(movieId, dateStr)

            result.onSuccess { showtimes ->
                val effectiveDate = date ?: LocalDate.now()
                val dates = buildDateList(effectiveDate)

                if (showtimes.isEmpty()) {
                    _uiState.update {
                        ShowtimeUiState.Success(
                            dates = dates,
                            selectedDate = effectiveDate,
                            roomShowtimes = emptyList()
                        )
                    }
                } else {
                    val grouped = showtimes
                        .sortedBy { it.startTime }
                        .groupBy { it.roomName }
                        .map { (roomName, items) -> RoomShowtimes(roomName, items) }

                    _uiState.update {
                        ShowtimeUiState.Success(
                            dates = dates,
                            selectedDate = effectiveDate,
                            roomShowtimes = grouped
                        )
                    }
                }
            }.onFailure { error ->
                _uiState.update {
                    ShowtimeUiState.Error(error.message ?: "Đã xảy ra lỗi")
                }
            }
        }
    }

    private fun buildDateList(selectedDate: LocalDate): List<DateItem> {
        val today = LocalDate.now()
        return (0..6).map { offset ->
            val d = today.plusDays(offset.toLong())
            DateItem(
                date = d,
                label = if (offset == 0) "Hôm nay" else d.format(DateTimeFormatter.ofPattern("dd/MM")),
                isSelected = d == selectedDate
            )
        }
    }
}
