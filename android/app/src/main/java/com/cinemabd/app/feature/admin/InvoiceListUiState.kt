package com.cinemabd.app.feature.admin

import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail

sealed interface InvoiceListUiState {
    data object Loading : InvoiceListUiState
    data class Error(val message: String) : InvoiceListUiState
    data class Empty(
        val statuses: List<String>,
        val selectedStatus: String? = null,
        val selectedPaymentDate: String? = null
    ) : InvoiceListUiState
    data class Success(
        val invoices: List<AdminInvoiceDetail>,
        val filteredInvoices: List<AdminInvoiceDetail>,
        val statuses: List<String>,
        val selectedStatus: String? = null,
        val selectedPaymentDate: String? = null
    ) : InvoiceListUiState
}
