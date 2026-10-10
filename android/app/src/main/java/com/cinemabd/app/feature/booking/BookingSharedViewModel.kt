package com.cinemabd.app.feature.booking

import androidx.lifecycle.ViewModel
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import javax.inject.Inject

data class BookingCartState(
    val showtimeId: String = "",
    val movieTitle: String = "",
    val showDate: String = "",
    val startTime: String = "",
    val roomName: String = "",
    val selectedSeats: List<String> = emptyList(),
    val ticketTotal: Double = 0.0,
    val selectedCombos: Map<String, Int> = emptyMap(),
    val comboNames: Map<String, String> = emptyMap(),
    val comboPrices: Map<String, Double> = emptyMap(),
    val comboTotal: Double = 0.0,
    val totalAmount: Double = 0.0
)

@HiltViewModel
class BookingSharedViewModel @Inject constructor() : ViewModel() {

    companion object {
        const val MAX_COMBO_QUANTITY = 20
    }

    private val _cartState = MutableStateFlow(BookingCartState())
    val cartState: StateFlow<BookingCartState> = _cartState.asStateFlow()

    fun setShowtimeInfo(
        showtimeId: String,
        movieTitle: String = "",
        showDate: String = "",
        startTime: String = "",
        roomName: String = ""
    ) {
        _cartState.update { current ->
            current.copy(
                showtimeId = showtimeId,
                movieTitle = movieTitle.ifBlank { current.movieTitle },
                showDate = showDate.ifBlank { current.showDate },
                startTime = startTime.ifBlank { current.startTime },
                roomName = roomName.ifBlank { current.roomName }
            )
        }
    }

    fun updateSeats(showtimeId: String, seats: List<String>, ticketTotal: Double) {
        _cartState.update { current ->
            val comboTotal = calculateComboTotal(current.selectedCombos, current.comboPrices)
            current.copy(
                showtimeId = showtimeId,
                selectedSeats = seats,
                ticketTotal = ticketTotal,
                comboTotal = comboTotal,
                totalAmount = ticketTotal + comboTotal
            )
        }
    }

    fun updateComboQuantity(
        comboId: String,
        delta: Int,
        unitPrice: Double,
        comboName: String? = null
    ) {
        _cartState.update { current ->
            val currentQty = current.selectedCombos[comboId] ?: 0
            val targetQty = (currentQty + delta).coerceIn(0, MAX_COMBO_QUANTITY)
            val updatedCombos = current.selectedCombos.toMutableMap()
            val updatedNames = current.comboNames.toMutableMap()
            if (targetQty <= 0) {
                updatedCombos.remove(comboId)
                updatedNames.remove(comboId)
            } else {
                updatedCombos[comboId] = targetQty
                if (!comboName.isNullOrBlank()) {
                    updatedNames[comboId] = comboName
                }
            }

            val updatedPrices = current.comboPrices + (comboId to unitPrice)
            val comboTotal = calculateComboTotal(updatedCombos, updatedPrices)
            current.copy(
                selectedCombos = updatedCombos,
                comboNames = updatedNames,
                comboPrices = updatedPrices,
                comboTotal = comboTotal,
                totalAmount = current.ticketTotal + comboTotal
            )
        }
    }

    fun clearCombos() {
        _cartState.update { current ->
            current.copy(
                selectedCombos = emptyMap(),
                comboNames = emptyMap(),
                comboTotal = 0.0,
                totalAmount = current.ticketTotal
            )
        }
    }

    fun clearCart() {
        _cartState.value = BookingCartState()
    }

    fun getFormattedCombosQuery(voucherCode: String? = null): String {
        val comboParts = _cartState.value.selectedCombos.entries
            .filter { it.value > 0 }
            .map { "${it.key}:${it.value}" }
            .toMutableList()

        if (!voucherCode.isNullOrBlank()) {
            comboParts.add("voucher:${voucherCode.trim().uppercase()}")
        }

        return comboParts.joinToString(",")
    }

    private fun calculateComboTotal(
        combos: Map<String, Int>,
        prices: Map<String, Double>
    ): Double {
        return combos.entries.sumOf { (id, qty) ->
            (prices[id] ?: 0.0) * qty
        }
    }
}
