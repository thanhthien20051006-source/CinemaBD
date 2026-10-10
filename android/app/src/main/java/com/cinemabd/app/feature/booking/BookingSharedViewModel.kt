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
    val selectedSeats: List<String> = emptyList(),
    val ticketTotal: Double = 0.0,
    val selectedCombos: Map<String, Int> = emptyMap(),
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

    fun updateComboQuantity(comboId: String, delta: Int, unitPrice: Double) {
        _cartState.update { current ->
            val currentQty = current.selectedCombos[comboId] ?: 0
            val targetQty = (currentQty + delta).coerceIn(0, MAX_COMBO_QUANTITY)
            val updatedCombos = current.selectedCombos.toMutableMap()
            if (targetQty <= 0) {
                updatedCombos.remove(comboId)
            } else {
                updatedCombos[comboId] = targetQty
            }

            val updatedPrices = current.comboPrices + (comboId to unitPrice)
            val comboTotal = calculateComboTotal(updatedCombos, updatedPrices)
            current.copy(
                selectedCombos = updatedCombos,
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
                comboTotal = 0.0,
                totalAmount = current.ticketTotal
            )
        }
    }

    fun getFormattedCombosQuery(): String {
        return _cartState.value.selectedCombos.entries
            .filter { it.value > 0 }
            .joinToString(",") { "${it.key}:${it.value}" }
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
