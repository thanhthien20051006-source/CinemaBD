package com.cinemabd.app.feature.booking

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.SeatHoldResult
import com.cinemabd.app.core.network.model.SeatResponse
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.data.repository.BookingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.time.Instant
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
        val errorMessage: String? = null,
        val holdRemainingSeconds: Int = 0
    ) : SeatMapUiState
    data class Error(val message: String) : SeatMapUiState
}

@HiltViewModel
class SeatMapViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val bookingsRepository: BookingsRepository
) : ViewModel() {

    companion object {
        const val POLL_INTERVAL_MS = 4000L
        const val MAX_SEATS = 8
    }

    val showtimeId: String = checkNotNull(savedStateHandle["showtimeId"])

    private val _uiState = MutableStateFlow<SeatMapUiState>(SeatMapUiState.Loading)
    val uiState: StateFlow<SeatMapUiState> = _uiState.asStateFlow()

    private val _eventFlow = MutableSharedFlow<String>(extraBufferCapacity = 64)
    val eventFlow: SharedFlow<String> = _eventFlow.asSharedFlow()

    private var pollingJob: Job? = null
    private var countdownJob: Job? = null
    private var heldSeatCodes: List<String> = emptyList()
    private var navigatedForward = false

    init {
        loadSeats()
    }

    fun startPolling() {
        if (pollingJob?.isActive == true) return
        pollingJob = viewModelScope.launch {
            while (isActive) {
                delay(POLL_INTERVAL_MS)
                pollSeats()
            }
        }
    }

    fun stopPolling() {
        pollingJob?.cancel()
        pollingJob = null
    }

    override fun onCleared() {
        super.onCleared()
        stopPolling()
        countdownJob?.cancel()
        releaseHeldSeats()
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
        val isDeselecting = currentSelected.contains(seatId)

        // Kiểm tra giới hạn tối đa ghế
        if (!isDeselecting && currentSelected.size >= MAX_SEATS) {
            _eventFlow.tryEmit("Tối đa $MAX_SEATS ghế mỗi lần đặt")
            return
        }

        val newSelected = if (isDeselecting) {
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

        _uiState.update { current ->
            if (current is SeatMapUiState.Success) {
                current.copy(
                    seats = updatedSeats,
                    selectedSeats = newSelected,
                    totalAmount = newTotal,
                    errorMessage = null
                )
            } else current
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
                heldSeatCodes = holdResult.heldSeats.ifEmpty { seatCodes }
                val expiresAtMillis = holdResult.expiresAt?.let { parseExpiresAt(it) }

                _uiState.update {
                    if (it is SeatMapUiState.Success) it.copy(
                        isHolding = false,
                        holdRemainingSeconds = if (expiresAtMillis != null) {
                            ((expiresAtMillis - System.currentTimeMillis()) / 1000).toInt().coerceAtLeast(0)
                        } else 0
                    ) else it
                }

                if (expiresAtMillis != null) {
                    startCountdown(expiresAtMillis)
                }

                navigatedForward = true
                onSuccess(holdResult)
            }.onFailure { error ->
                val errorMsg = error.toUserFriendlyMessage("Không thể giữ ghế. Vui lòng chọn ghế khác.")
                _eventFlow.emit(errorMsg)
                refreshSeatsAfterError(errorMsg)
            }
        }
    }

    /**
     * Đánh dấu đã điều hướng tiếp, không cần release ghế khi rời màn hình.
     */
    fun markNavigatedForward() {
        navigatedForward = true
    }

    /**
     * Nhả ghế đang giữ khi user rời màn hình (back/destroy).
     * Không release nếu user đã điều hướng tiếp sang ComboScreen.
     */
    fun releaseHeldSeats() {
        if (navigatedForward || heldSeatCodes.isEmpty()) return
        val seats = heldSeatCodes.toList()
        heldSeatCodes = emptyList()
        countdownJob?.cancel()
        // Dùng CoroutineScope riêng vì viewModelScope bị cancel khi onCleared
        CoroutineScope(Dispatchers.IO).launch {
            withContext(NonCancellable) {
                bookingsRepository.releaseSeats(showtimeId, seats)
            }
        }
    }

    private fun parseExpiresAt(expiresAt: String): Long? {
        return try {
            Instant.parse(expiresAt).toEpochMilli()
        } catch (_: Exception) {
            null
        }
    }

    private fun startCountdown(expiresAtMillis: Long) {
        countdownJob?.cancel()
        countdownJob = viewModelScope.launch {
            while (isActive) {
                val remaining = ((expiresAtMillis - System.currentTimeMillis()) / 1000).toInt()
                if (remaining <= 0) {
                    _uiState.update {
                        if (it is SeatMapUiState.Success) it.copy(
                            holdRemainingSeconds = 0,
                            selectedSeats = emptySet(),
                            totalAmount = 0.0
                        ) else it
                    }
                    heldSeatCodes = emptyList()
                    _eventFlow.emit("Hết thời gian giữ ghế. Vui lòng chọn lại.")
                    loadSeats()
                    break
                }
                _uiState.update {
                    if (it is SeatMapUiState.Success) it.copy(holdRemainingSeconds = remaining) else it
                }
                delay(1000L)
            }
        }
    }

    private fun loadSeats() {
        viewModelScope.launch {
            _uiState.update { SeatMapUiState.Loading }

            val result = bookingsRepository.getSeats(showtimeId)
            result.onSuccess { seatResponses ->
                val (seatUiModels, maxCols) = mapSeats(seatResponses, emptySet())

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
                    SeatMapUiState.Error(error.toUserFriendlyMessage("Không thể tải sơ đồ ghế. Vui lòng thử lại."))
                }
            }
        }
    }

    private suspend fun pollSeats() {
        val currentState = _uiState.value as? SeatMapUiState.Success ?: return
        if (currentState.isHolding) return

        val result = bookingsRepository.getSeats(showtimeId)
        result.onSuccess { seatResponses ->
            val latestState = _uiState.value as? SeatMapUiState.Success ?: return@onSuccess
            if (latestState.isHolding) return@onSuccess

            val currentSelected = latestState.selectedSeats
            val stillSelected = mutableSetOf<String>()
            val lostSeats = mutableListOf<String>()

            for (seatId in currentSelected) {
                val matching = seatResponses.firstOrNull { it.id == seatId }
                if (matching != null && !matching.isBooked && matching.status.equals("Available", ignoreCase = true)) {
                    stillSelected.add(seatId)
                } else {
                    lostSeats.add(seatId)
                }
            }

            if (lostSeats.isNotEmpty()) {
                val seatNames = lostSeats.joinToString(", ")
                val message = if (lostSeats.size == 1) {
                    "Ghế $seatNames đã có người mua hoặc giữ chỗ, hệ thống đã tự động bỏ chọn."
                } else {
                    "Các ghế $seatNames đã có người mua hoặc giữ chỗ, hệ thống đã tự động bỏ chọn."
                }
                _eventFlow.emit(message)
            }

            val (seatUiModels, maxCols) = mapSeats(seatResponses, stillSelected)
            val newTotal = seatUiModels.filter { it.id in stillSelected }.sumOf { it.price }

            _uiState.update { current ->
                if (current is SeatMapUiState.Success && !current.isHolding) {
                    current.copy(
                        seats = seatUiModels,
                        maxColumns = maxCols,
                        selectedSeats = stillSelected,
                        totalAmount = newTotal
                    )
                } else current
            }
        }.onFailure {
            // Im lặng bỏ qua lỗi polling mạng để không làm gián đoạn trải nghiệm người dùng
        }
    }

    private suspend fun refreshSeatsAfterError(errorMessage: String) {
        val currentState = _uiState.value as? SeatMapUiState.Success
        val result = bookingsRepository.getSeats(showtimeId)

        result.onSuccess { seatResponses ->
            // Giữ lại các ghế đã chọn nếu chúng vẫn Available
            val oldSelected = currentState?.selectedSeats ?: emptySet()
            val validSelected = oldSelected.filter { id ->
                val matching = seatResponses.firstOrNull { it.id == id }
                matching != null && !matching.isBooked && matching.status.equals("Available", ignoreCase = true)
            }.toSet()

            val (seatUiModels, maxCols) = mapSeats(seatResponses, validSelected)
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

    private fun mapSeats(
        seatResponses: List<SeatResponse>,
        selectedSeats: Set<String>
    ): Pair<List<SeatUiModel>, Int> {
        val sorted = seatResponses.sortedWith(
            compareBy<SeatResponse> { it.row }
                .thenBy { it.column.toIntOrNull() ?: 0 }
        )
        val maxCols = sorted.mapNotNull { it.column.toIntOrNull() }.maxOrNull() ?: 6

        val seatUiModels = sorted.map { seat ->
            val isVip = seat.seatType?.equals("VIP", ignoreCase = true) == true
            val isSelected = seat.id in selectedSeats
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

        return Pair(seatUiModels, maxCols)
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
