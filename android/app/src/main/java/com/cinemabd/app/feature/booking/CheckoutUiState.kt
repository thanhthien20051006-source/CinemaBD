package com.cinemabd.app.feature.booking

enum class PaymentMethod {
    DEMO,
    VNPAY
}

sealed interface CheckoutUiState {
    data object Loading : CheckoutUiState

    data class Ready(
        val showtimeId: String = "",
        val movieTitle: String = "",
        val showDate: String = "",
        val startTime: String = "",
        val roomName: String = "",
        val seats: List<String> = emptyList(),
        val ticketTotal: Double = 0.0,
        val selectedCombos: Map<String, Int> = emptyMap(),
        val comboNames: Map<String, String> = emptyMap(),
        val comboPrices: Map<String, Double> = emptyMap(),
        val comboTotal: Double = 0.0,
        val subtotal: Double = 0.0,
        val voucherCode: String = "",
        val discountAmount: Double = 0.0,
        val finalTotal: Double = 0.0,
        val voucherMessage: String? = null,
        val selectedPaymentMethod: PaymentMethod = PaymentMethod.DEMO,
        val isProcessing: Boolean = false,
        val errorMessage: String? = null
    ) : CheckoutUiState

    data class Success(val transactionRef: String) : CheckoutUiState
    data class Error(val message: String) : CheckoutUiState
}
