package com.cinemabd.app.feature.booking

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.SeatHoldResult
import com.cinemabd.app.core.network.model.SeatResponse
import com.cinemabd.app.data.repository.BookingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

enum class SeatDisplayStatus {
    AVAILABLE_NORMAL,
    AVAILABLE_VIP,
    SELECTED,
    SOLD,
    HELD_BY_OTHER
}

data class SeatUiModel(
    val id: String,
    val row: String,
    val column: String,
    val seatType: String?,
    val isVip: Boolean,
    val isBooked: Boolean,
    val status: String,
    val price: Double,
    val displayStatus: SeatDisplayStatus
)

sealed interface SeatMapUiState {
    data object Loading : SeatMapUiState
    data class Success(
        val showtimeId: String,
        val seats: List<SeatUiModel>,
        val maxColumns: Int,
        val selectedSeats: Set<String>,
        val totalAmount: Double,
        val isHolding: Boolean = false,
        val errorMessage: String? = null
    ) : SeatMapUiState
    data class Error(val message: String) : SeatMapUiState
}

@HiltViewModel
class SeatMapViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val bookingsRepository: BookingsRepository
) : ViewModel() {

    val showtimeId: String = checkNotNull(savedStateHandle["showtimeId"])

    private val _uiState = MutableStateFlow<SeatMapUiState>(SeatMapUiState.Loading)
    val uiState: StateFlow<SeatMapUiState> = _uiState.asStateFlow()

    private val _eventFlow = MutableSharedFlow<String>()
    val eventFlow: SharedFlow<String> = _eventFlow.asSharedFlow()

    init {
        loadSeats()
    }

    fun retry() {
        loadSeats()
    }

    fun clearErrorMessage() {
        _uiState.update {
            if (it is SeatMapUiState.Success) it.copy(errorMessage = null) else it
        }
    }

    fun toggleSeat(seatId: String) {
        val currentState = _uiState.value as? SeatMapUiState.Success ?: return
        val seat = currentState.seats.firstOrNull { it.id == seatId } ?: return

        // Chỉ cho phép chọn ghế đang khả dụng (không bị bán/khóa/giữ bởi người khác)
        if (seat.isBooked || !seat.status.equals("Available", ignoreCase = true)) {
            return
        }

        val currentSelected = currentState.selectedSeats
        val newSelected = if (currentSelected.contains(seatId)) {
            currentSelected - seatId
        } else {
            currentSelected + seatId
        }

        val newTotal = currentState.seats
            .filter { it.id in newSelected }
            .sumOf { it.price }

        val updatedSeats = currentState.seats.map { item ->
            item.copy(displayStatus = resolveDisplayStatus(item, newSelected))
        }

        _uiState.update {
            currentState.copy(
                seats = updatedSeats,
                selectedSeats = newSelected,
                totalAmount = newTotal,
                errorMessage = null
            )
        }
    }

    fun holdSeats(onSuccess: (SeatHoldResult) -> Unit) {
        val currentState = _uiState.value as? SeatMapUiState.Success ?: return
        if (currentState.selectedSeats.isEmpty() || currentState.isHolding) return

        viewModelScope.launch {
            _uiState.update {
                if (it is SeatMapUiState.Success) it.copy(isHolding = true, errorMessage = null) else it
            }

            val seatCodes = currentState.selectedSeats.toList()
            val result = bookingsRepository.holdSeats(showtimeId, seatCodes)

            result.onSuccess { holdResult ->
                _uiState.update {
                    if (it is SeatMapUiState.Success) it.copy(isHolding = false) else it
                }
                onSuccess(holdResult)
            }.onFailure { error ->
                val errorMsg = error.message ?: "Không thể giữ ghế. Vui lòng chọn ghế khác."
                _eventFlow.emit(errorMsg)
                refreshSeatsAfterError(errorMsg)
            }
        }
    }

    private fun loadSeats() {
        viewModelScope.launch {
            _uiState.update { SeatMapUiState.Loading }

            val result = bookingsRepository.getSeats(showtimeId)
            result.onSuccess { seatResponses ->
                val sorted = seatResponses.sortedWith(
                    compareBy<SeatResponse> { it.row }
                        .thenBy { it.column.toIntOrNull() ?: 0 }
                )
                val maxCols = sorted.mapNotNull { it.column.toIntOrNull() }.maxOrNull() ?: 6

                val seatUiModels = sorted.map { seat ->
                    val isVip = seat.seatType?.equals("VIP", ignoreCase = true) == true
                    SeatUiModel(
                        id = seat.id,
                        row = seat.row,
                        column = seat.column,
                        seatType = seat.seatType,
                        isVip = isVip,
                        isBooked = seat.isBooked,
                        status = seat.status,
                        price = seat.price,
                        displayStatus = resolveDisplayStatus(seat.isBooked, seat.status, isVip, false)
                    )
                }

                _uiState.update {
                    SeatMapUiState.Success(
                        showtimeId = showtimeId,
                        seats = seatUiModels,
                        maxColumns = maxCols,
                        selectedSeats = emptySet(),
                        totalAmount = 0.0
                    )
                }
            }.onFailure { error ->
                _uiState.update {
                    SeatMapUiState.Error(error.message ?: "Không thể tải sơ đồ ghế.")
                }
            }
        }
    }

    private suspend fun refreshSeatsAfterError(errorMessage: String) {
        val currentState = _uiState.value as? SeatMapUiState.Success
        val result = bookingsRepository.getSeats(showtimeId)

        result.onSuccess { seatResponses ->
            val sorted = seatResponses.sortedWith(
                compareBy<SeatResponse> { it.row }
                    .thenBy { it.column.toIntOrNull() ?: 0 }
            )
            val maxCols = sorted.mapNotNull { it.column.toIntOrNull() }.maxOrNull() ?: 6

            // Giữ lại các ghế đã chọn nếu chúng vẫn Available
            val oldSelected = currentState?.selectedSeats ?: emptySet()
            val validSelected = oldSelected.filter { id ->
                val matching = sorted.firstOrNull { it.id == id }
                matching != null && !matching.isBooked && matching.status.equals("Available", ignoreCase = true)
            }.toSet()

            val seatUiModels = sorted.map { seat ->
                val isVip = seat.seatType?.equals("VIP", ignoreCase = true) == true
                val isSelected = seat.id in validSelected
                SeatUiModel(
                    id = seat.id,
                    row = seat.row,
                    column = seat.column,
                    seatType = seat.seatType,
                    isVip = isVip,
                    isBooked = seat.isBooked,
                    status = seat.status,
                    price = seat.price,
                    displayStatus = resolveDisplayStatus(seat.isBooked, seat.status, isVip, isSelected)
                )
            }

            val newTotal = seatUiModels.filter { it.id in validSelected }.sumOf { it.price }

            _uiState.update {
                SeatMapUiState.Success(
                    showtimeId = showtimeId,
                    seats = seatUiModels,
                    maxColumns = maxCols,
                    selectedSeats = validSelected,
                    totalAmount = newTotal,
                    isHolding = false,
                    errorMessage = errorMessage
                )
            }
        }.onFailure {
            _uiState.update {
                if (it is SeatMapUiState.Success) {
                    it.copy(isHolding = false, errorMessage = errorMessage)
                } else it
            }
        }
    }

    private fun resolveDisplayStatus(
        isBooked: Boolean,
        status: String,
        isVip: Boolean,
        isSelected: Boolean
    ): SeatDisplayStatus {
        if (isSelected) return SeatDisplayStatus.SELECTED
        if (isBooked || status.equals("Sold", ignoreCase = true) || status.equals("CheckedIn", ignoreCase = true)) {
            return SeatDisplayStatus.SOLD
        }
        if (status.equals("Held", ignoreCase = true) || status.equals("Blocked", ignoreCase = true) || status.equals("Pending", ignoreCase = true)) {
            return SeatDisplayStatus.HELD_BY_OTHER
        }
        return if (isVip) SeatDisplayStatus.AVAILABLE_VIP else SeatDisplayStatus.AVAILABLE_NORMAL
    }

    private fun resolveDisplayStatus(seat: SeatUiModel, selectedSeats: Set<String>): SeatDisplayStatus {
        val isSelected = seat.id in selectedSeats
        return resolveDisplayStatus(seat.isBooked, seat.status, seat.isVip, isSelected)
    }
}
