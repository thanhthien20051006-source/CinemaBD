package com.cinemabd.app.feature.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import com.cinemabd.app.feature.admin.model.AdminInvoiceLineItem

@Composable
fun InvoiceDetailScreen(
    viewModel: InvoiceDetailViewModel = hiltViewModel(),
    onBack: () -> Unit
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    Scaffold(topBar = {
        AdminTopBar(title = stringResource(R.string.admin_invoice_detail_title), onBack = onBack)
    }) { padding ->
        when (val state = uiState) {
            InvoiceDetailUiState.Loading -> Column(Modifier.fillMaxSize().padding(padding)) {
                LoadingContent()
            }
            is InvoiceDetailUiState.Error -> Column(Modifier.fillMaxSize().padding(padding)) {
                ErrorContent(state.message, viewModel::loadDetail)
            }
            InvoiceDetailUiState.Empty -> Column(Modifier.fillMaxSize().padding(padding)) {
                EmptyContent(stringResource(R.string.admin_invoice_not_found))
            }
            is InvoiceDetailUiState.Success -> InvoiceDetailContent(state.invoice, Modifier.padding(padding))
        }
    }
}

@Composable
private fun InvoiceDetailContent(invoice: AdminInvoiceDetail, modifier: Modifier = Modifier) {
    LazyColumn(
        modifier = modifier.fillMaxSize().padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Card(colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.48f))) {
                Column(Modifier.fillMaxWidth().padding(16.dp)) {
                    DetailInfoRow(stringResource(R.string.admin_invoice_id), invoice.invoiceId)
                    DetailInfoRow(stringResource(R.string.admin_invoice_status), invoice.status ?: "—")
                    DetailInfoRow(stringResource(R.string.admin_invoice_customer), invoice.customerName ?: "—")
                    DetailInfoRow(stringResource(R.string.admin_invoice_movie), invoice.movieTitle ?: "—")
                    DetailInfoRow(stringResource(R.string.admin_invoice_showtime),
                        com.cinemabd.app.core.utils.DateTimeUtils.formatShowDateTime(invoice.showDate, invoice.startTime))
                    DetailInfoRow(stringResource(R.string.admin_invoice_payment_date), formatAdminDate(invoice.paymentDate))
                    DetailInfoRow(stringResource(R.string.admin_invoice_transaction), invoice.transactionRef ?: "—")
                    DetailInfoRow(stringResource(R.string.admin_invoice_checked_in),
                        stringResource(if (invoice.isCheckedIn) R.string.admin_yes else R.string.admin_no))
                    if (invoice.checkedInAt != null) {
                        DetailInfoRow(stringResource(R.string.admin_invoice_checked_in_at), formatAdminDate(invoice.checkedInAt))
                    }
                    if (!invoice.note.isNullOrBlank()) {
                        DetailInfoRow(stringResource(R.string.admin_invoice_note), invoice.note)
                    }
                    HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                    DetailInfoRow(
                        stringResource(R.string.admin_invoice_total),
                        DateTimeUtils.formatCurrency(invoice.totalAmount),
                        emphasize = true
                    )
                }
            }
        }
        item {
            Text(
                stringResource(R.string.admin_invoice_line_items),
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.padding(top = 4.dp)
            )
        }
        if (invoice.lineItems.isEmpty()) {
            item { Text(stringResource(R.string.admin_invoice_no_line_items), color = MaterialTheme.colorScheme.onSurfaceVariant) }
        } else {
            items(invoice.lineItems, key = { it.id }) { lineItem -> InvoiceLineItemCard(lineItem) }
        }
    }
}

@Composable
private fun InvoiceLineItemCard(lineItem: AdminInvoiceLineItem) {
    Card(colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.35f))) {
        Column(Modifier.fillMaxWidth().padding(14.dp)) {
            Text(lineItem.itemName, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            DetailInfoRow(stringResource(R.string.admin_invoice_line_type), lineItem.lineType)
            DetailInfoRow(stringResource(R.string.admin_invoice_line_quantity), lineItem.quantity.toString())
            DetailInfoRow(stringResource(R.string.admin_invoice_line_price), DateTimeUtils.formatCurrency(lineItem.unitPrice))
            DetailInfoRow(stringResource(R.string.admin_invoice_checked_in),
                stringResource(if (lineItem.isCheckedIn) R.string.admin_yes else R.string.admin_no))
            if (!lineItem.ticketId.isNullOrBlank()) {
                DetailInfoRow(stringResource(R.string.admin_invoice_line_ticket), lineItem.ticketId)
            }
            if (!lineItem.comboId.isNullOrBlank()) {
                DetailInfoRow(stringResource(R.string.admin_invoice_line_combo), lineItem.comboId)
            }
            if (lineItem.checkedInAt != null) {
                DetailInfoRow(stringResource(R.string.admin_invoice_checked_in_at), formatAdminDate(lineItem.checkedInAt))
            }
        }
    }
}

@Composable
private fun DetailInfoRow(label: String, value: String, emphasize: Boolean = false) {
    Row(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodyMedium, fontWeight = if (emphasize) FontWeight.Bold else FontWeight.Medium)
    }
}
