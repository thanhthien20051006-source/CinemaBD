package com.cinemabd.app.feature.admin

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InvoiceListScreen(
    viewModel: InvoiceListViewModel = hiltViewModel(),
    onInvoiceClick: (String) -> Unit
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    var showStatusMenu by remember { mutableStateOf(false) }
    var showDatePicker by remember { mutableStateOf(false) }
    val selectedDate = when (val state = uiState) {
        is InvoiceListUiState.Success -> state.selectedPaymentDate
        is InvoiceListUiState.Empty -> state.selectedPaymentDate
        else -> null
    }
    val datePickerState = key(selectedDate) {
        rememberDatePickerState(
            initialSelectedDateMillis = selectedDate?.let {
                runCatching { LocalDate.parse(it).atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli() }.getOrNull()
            }
        )
    }

    if (showDatePicker) {
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(
                    onClick = {
                        datePickerState.selectedDateMillis?.let { millis ->
                            val date = Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate().toString()
                            viewModel.selectPaymentDate(date)
                        }
                        showDatePicker = false
                    }
                ) { Text(stringResource(R.string.admin_filter_apply)) }
            },
            dismissButton = {
                Row {
                    TextButton(onClick = {
                        viewModel.selectPaymentDate(null)
                        showDatePicker = false
                    }) { Text(stringResource(R.string.admin_filter_clear)) }
                    TextButton(onClick = { showDatePicker = false }) {
                        Text(stringResource(R.string.profile_logout_cancel))
                    }
                }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    Scaffold(topBar = {
        AdminTopBar(title = stringResource(R.string.admin_invoices_title))
    }) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp)
        ) {
            val statuses = when (val state = uiState) {
                is InvoiceListUiState.Success -> state.statuses
                is InvoiceListUiState.Empty -> state.statuses
                else -> emptyList()
            }
            val selectedStatus = when (val state = uiState) {
                is InvoiceListUiState.Success -> state.selectedStatus
                is InvoiceListUiState.Empty -> state.selectedStatus
                else -> null
            }
            Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = 12.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box {
                    OutlinedButton(onClick = { showStatusMenu = true }) {
                        Text(selectedStatus ?: stringResource(R.string.admin_filter_status))
                    }
                    DropdownMenu(expanded = showStatusMenu, onDismissRequest = { showStatusMenu = false }) {
                        DropdownMenuItem(
                            text = { Text(stringResource(R.string.admin_filter_all)) },
                            onClick = {
                                viewModel.selectStatus(null)
                                showStatusMenu = false
                            }
                        )
                        statuses.forEach { status ->
                            DropdownMenuItem(
                                text = { Text(status) },
                                onClick = {
                                    viewModel.selectStatus(status)
                                    showStatusMenu = false
                                }
                            )
                        }
                    }
                }
                OutlinedButton(onClick = { showDatePicker = true }) {
                    Icon(Icons.Default.DateRange, contentDescription = null)
                    Text(
                        text = selectedDate?.let(::formatAdminDate)
                            ?: stringResource(R.string.admin_filter_payment_date),
                        modifier = Modifier.padding(start = 6.dp)
                    )
                }
            }

            when (val state = uiState) {
                InvoiceListUiState.Loading -> LoadingContent()
                is InvoiceListUiState.Error -> ErrorContent(state.message, viewModel::loadInvoices)
                is InvoiceListUiState.Empty -> EmptyContent(
                    message = if (state.statuses.isEmpty()) stringResource(R.string.admin_invoices_empty)
                    else stringResource(R.string.admin_invoices_filter_empty),
                    onRetry = viewModel::loadInvoices
                )
                is InvoiceListUiState.Success -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(state.filteredInvoices, key = { it.invoiceId }) { invoice ->
                        InvoiceListItem(invoice = invoice, onClick = { onInvoiceClick(invoice.invoiceId) })
                    }
                    item { Spacer(modifier = Modifier.height(8.dp)) }
                }
            }
        }
    }
}

@Composable
private fun InvoiceListItem(invoice: AdminInvoiceDetail, onClick: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.48f))
    ) {
        Column(modifier = Modifier.fillMaxWidth().padding(16.dp)) {
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(invoice.invoiceId, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                Text(invoice.status ?: "—", color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.labelLarge)
            }
            Spacer(modifier = Modifier.height(4.dp))
            Text(invoice.movieTitle ?: stringResource(R.string.admin_value_unavailable), style = MaterialTheme.typography.bodyLarge)
            Text(invoice.customerName ?: stringResource(R.string.admin_value_unavailable), style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.height(8.dp))
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(formatAdminDate(invoice.paymentDate), style = MaterialTheme.typography.bodySmall)
                Text(DateTimeUtils.formatCurrency(invoice.totalAmount), fontWeight = FontWeight.SemiBold)
            }
        }
    }
}

@Composable
internal fun LoadingContent() {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
internal fun ErrorContent(message: String, onRetry: () -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(modifier = Modifier.height(12.dp))
        Button(onClick = onRetry) {
            Icon(Icons.Default.Refresh, contentDescription = null)
            Text(stringResource(R.string.retry), modifier = Modifier.padding(start = 8.dp))
        }
    }
}

@Composable
internal fun EmptyContent(message: String, onRetry: (() -> Unit)? = null) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(message, color = MaterialTheme.colorScheme.onSurfaceVariant)
        if (onRetry != null) {
            Spacer(modifier = Modifier.height(12.dp))
            TextButton(onClick = onRetry) { Text(stringResource(R.string.retry)) }
        }
    }
}

internal fun formatAdminDate(value: String): String = try {
    val dateText = value.substringBefore('T')
    LocalDate.parse(dateText).format(java.time.format.DateTimeFormatter.ofPattern("dd/MM/yyyy"))
} catch (_: Exception) {
    value
}
