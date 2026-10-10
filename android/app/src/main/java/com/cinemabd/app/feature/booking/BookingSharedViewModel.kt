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
class BookingSharedViewModel @Inject constructor(
    private val cartManager: BookingCartManager
) : ViewModel() {

    companion object {
        const val MAX_COMBO_QUANTITY = BookingCartManager.MAX_COMBO_QUANTITY
    }

    val cartState: StateFlow<BookingCartState> = cartManager.cartState

    fun setShowtimeInfo(
        showtimeId: String,
        movieTitle: String = "",
        showDate: String = "",
        startTime: String = "",
        roomName: String = ""
    ) {
        cartManager.setShowtimeInfo(showtimeId, movieTitle, showDate, startTime, roomName)
    }

    fun updateSeats(showtimeId: String, seats: List<String>, ticketTotal: Double) {
        cartManager.updateSeats(showtimeId, seats, ticketTotal)
    }

    fun updateComboQuantity(
        comboId: String,
        delta: Int,
        unitPrice: Double,
        comboName: String? = null
    ) {
        cartManager.updateComboQuantity(comboId, delta, unitPrice, comboName)
    }

    fun clearCombos() {
        cartManager.clearCombos()
    }

    fun clearCart() {
        cartManager.clearCart()
    }

    fun getFormattedCombosQuery(voucherCode: String? = null): String {
        return cartManager.getFormattedCombosQuery(voucherCode)
    }
}
