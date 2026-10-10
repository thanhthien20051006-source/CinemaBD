package com.cinemabd.app.feature.booking

import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.browser.customtabs.CustomTabsIntent
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.data.repository.BookingsRepository
import com.cinemabd.app.data.repository.PaymentsRepository
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

sealed interface CheckoutEvent {
    data class ShowToast(val message: String) : CheckoutEvent
    data class OpenUrl(val url: String) : CheckoutEvent
}

@HiltViewModel
class CheckoutViewModel @Inject constructor(
    private val bookingsRepository: BookingsRepository,
    private val paymentsRepository: PaymentsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<CheckoutUiState>(CheckoutUiState.Loading)
    val uiState: StateFlow<CheckoutUiState> = _uiState.asStateFlow()

    private val _eventFlow = MutableSharedFlow<CheckoutEvent>()
    val eventFlow: SharedFlow<CheckoutEvent> = _eventFlow.asSharedFlow()

    fun initFromCart(cartState: BookingCartState) {
        val subtotal = cartState.ticketTotal + cartState.comboTotal
        _uiState.update { current ->
            if (current is CheckoutUiState.Ready) {
                val discount = calculateEstimatedDiscount(current.voucherCode, subtotal)
                current.copy(
                    showtimeId = cartState.showtimeId,
                    movieTitle = cartState.movieTitle,
                    showDate = cartState.showDate,
                    startTime = cartState.startTime,
                    roomName = cartState.roomName,
                    seats = cartState.selectedSeats,
                    ticketTotal = cartState.ticketTotal,
                    selectedCombos = cartState.selectedCombos,
                    comboNames = cartState.comboNames,
                    comboPrices = cartState.comboPrices,
                    comboTotal = cartState.comboTotal,
                    subtotal = subtotal,
                    discountAmount = discount,
                    finalTotal = maxOf(0.0, subtotal - discount)
                )
            } else {
                CheckoutUiState.Ready(
                    showtimeId = cartState.showtimeId,
                    movieTitle = cartState.movieTitle,
                    showDate = cartState.showDate,
                    startTime = cartState.startTime,
                    roomName = cartState.roomName,
                    seats = cartState.selectedSeats,
                    ticketTotal = cartState.ticketTotal,
                    selectedCombos = cartState.selectedCombos,
                    comboNames = cartState.comboNames,
                    comboPrices = cartState.comboPrices,
                    comboTotal = cartState.comboTotal,
                    subtotal = subtotal,
                    finalTotal = subtotal
                )
            }
        }
    }

    fun selectPaymentMethod(method: PaymentMethod) {
        _uiState.update { current ->
            if (current is CheckoutUiState.Ready) {
                current.copy(selectedPaymentMethod = method, errorMessage = null)
            } else current
        }
    }

    fun applyVoucher(code: String) {
        _uiState.update { current ->
            if (current is CheckoutUiState.Ready) {
                val cleanCode = code.trim().uppercase()
                if (cleanCode.isBlank()) {
                    current.copy(
                        voucherCode = "",
                        discountAmount = 0.0,
                        finalTotal = current.subtotal,
                        voucherMessage = null
                    )
                } else {
                    val discount = calculateEstimatedDiscount(cleanCode, current.subtotal)
                    current.copy(
                        voucherCode = cleanCode,
                        discountAmount = discount,
                        finalTotal = maxOf(0.0, current.subtotal - discount),
                        voucherMessage = if (discount > 0) cleanCode else null
                    )
                }
            } else current
        }
    }

    fun submitPayment(context: Context, returnUrl: String? = null) {
        val state = _uiState.value as? CheckoutUiState.Ready ?: return
        if (state.seats.isEmpty()) {
            _uiState.update { state.copy(errorMessage = "Chưa chọn ghế.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { state.copy(isProcessing = true, errorMessage = null) }

            val comboParts = state.selectedCombos.entries
                .filter { it.value > 0 }
                .map { "${it.key}:${it.value}" }
                .toMutableList()
            if (state.voucherCode.isNotBlank()) {
                comboParts.add("voucher:${state.voucherCode}")
            }
            val comboQuery = if (comboParts.isEmpty()) null else comboParts.joinToString(",")

            val checkoutResult = bookingsRepository.checkout(
                showtimeId = state.showtimeId,
                seats = state.seats,
                combos = comboQuery,
                totalAmount = state.finalTotal,
                returnUrl = returnUrl
            )

            checkoutResult.onSuccess { response ->
                when (state.selectedPaymentMethod) {
                    PaymentMethod.DEMO -> {
                        val confirmResult = paymentsRepository.demoConfirm(
                            txnRef = response.transactionRef,
                            gateway = "MOMO"
                        )
                        confirmResult.onSuccess {
                            _uiState.value = CheckoutUiState.Success(response.transactionRef)
                        }.onFailure { err ->
                            _uiState.update {
                                state.copy(
                                    isProcessing = false,
                                    errorMessage = err.message ?: "Xác nhận thanh toán demo thất bại."
                                )
                            }
                        }
                    }

                    PaymentMethod.VNPAY -> {
                        _uiState.update { state.copy(isProcessing = false) }
                        try {
                            val customTabsIntent = CustomTabsIntent.Builder()
                                .setShowTitle(true)
                                .build()
                            customTabsIntent.launchUrl(context, Uri.parse(response.paymentUrl))
                        } catch (e: Exception) {
                            val browserIntent = Intent(Intent.ACTION_VIEW, Uri.parse(response.paymentUrl))
                            context.startActivity(browserIntent)
                        }
                    }
                }
            }.onFailure { err ->
                _uiState.update {
                    state.copy(
                        isProcessing = false,
                        errorMessage = err.message ?: "Không thể tạo phiên thanh toán."
                    )
                }
            }
        }
    }

    private fun calculateEstimatedDiscount(code: String, subtotal: Double): Double {
        val normalized = code.trim().uppercase()
        if (normalized.isBlank()) return 0.0

        val percentRegex = Regex("(GIAM|SALE|DISCOUNT)(\\d{1,2})$")
        val percentMatch = percentRegex.find(normalized)
        if (percentMatch != null) {
            val percent = percentMatch.groupValues[2].toDoubleOrNull() ?: 0.0
            return (subtotal * percent / 100.0)
        }

        val amountRegex = Regex("(\\d{4,})$")
        val amountMatch = amountRegex.find(normalized)
        if (amountMatch != null) {
            val amount = amountMatch.groupValues[1].toDoubleOrNull() ?: 0.0
            return minOf(subtotal, amount)
        }

        return minOf(subtotal, 20000.0)
    }
}
