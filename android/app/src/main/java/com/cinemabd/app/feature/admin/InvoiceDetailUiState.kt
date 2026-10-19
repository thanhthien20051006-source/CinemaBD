package com.cinemabd.app.feature.admin

import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail

sealed interface InvoiceDetailUiState {
    data object Loading : InvoiceDetailUiState
    data class Error(val message: String) : InvoiceDetailUiState
    data object Empty : InvoiceDetailUiState
    data class Success(val invoice: AdminInvoiceDetail) : InvoiceDetailUiState
}
