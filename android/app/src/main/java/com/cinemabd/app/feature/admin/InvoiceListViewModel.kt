package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import dagger.hilt.android.lifecycle.HiltViewModel
import java.time.LocalDate
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class InvoiceListViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow<InvoiceListUiState>(InvoiceListUiState.Loading)
    val uiState: StateFlow<InvoiceListUiState> = _uiState.asStateFlow()

    private var loadedInvoices: List<AdminInvoiceDetail> = emptyList()
    private var selectedStatus: String? = null
    private var selectedPaymentDate: String? = null
    private var hasLoaded = false

    init {
        loadInvoices()
    }

    fun loadInvoices() {
        viewModelScope.launch {
            _uiState.value = InvoiceListUiState.Loading
            adminRepository.getInvoices().onSuccess { response ->
                if (!response.success || response.data == null) {
                    _uiState.value = InvoiceListUiState.Error(
                        response.message.ifBlank { "Không thể tải danh sách hóa đơn." }
                    )
                    return@onSuccess
                }
                loadedInvoices = response.data
                hasLoaded = true
                publishFilteredInvoices()
            }.onFailure { error ->
                _uiState.value = InvoiceListUiState.Error(
                    error.toUserFriendlyMessage("Không thể tải danh sách hóa đơn.")
                )
            }
        }
    }

    fun selectStatus(status: String?) {
        selectedStatus = status
        if (hasLoaded) publishFilteredInvoices()
    }

    fun selectPaymentDate(date: String?) {
        selectedPaymentDate = date
        if (hasLoaded) publishFilteredInvoices()
    }

    private fun publishFilteredInvoices() {
        val statuses = loadedInvoices.mapNotNull { it.status?.takeIf(String::isNotBlank) }
            .distinct()
            .sorted()
        val filtered = loadedInvoices.filter { invoice ->
            (selectedStatus == null || invoice.status == selectedStatus) &&
                    (selectedPaymentDate == null || paymentDate(invoice.paymentDate) == selectedPaymentDate)
        }

        _uiState.value = if (filtered.isEmpty()) {
            InvoiceListUiState.Empty(
                statuses = statuses,
                selectedStatus = selectedStatus,
                selectedPaymentDate = selectedPaymentDate
            )
        } else {
            InvoiceListUiState.Success(
                invoices = loadedInvoices,
                filteredInvoices = filtered,
                statuses = statuses,
                selectedStatus = selectedStatus,
                selectedPaymentDate = selectedPaymentDate
            )
        }
    }

    private fun paymentDate(value: String): String = try {
        runCatching { OffsetDateTime.parse(value).toLocalDate() }
            .getOrElse { LocalDate.parse(value.substringBefore('T')) }
            .format(DateTimeFormatter.ISO_LOCAL_DATE)
    } catch (_: Exception) {
        value.substringBefore('T')
    }
}
