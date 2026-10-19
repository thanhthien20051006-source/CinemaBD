package com.cinemabd.app.feature.admin

import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.feature.admin.model.AdminRefundRequest

private enum class RefundActionType {
    APPROVE, REJECT
}

@Composable
fun AdminRefundScreen(
    viewModel: AdminRefundViewModel = hiltViewModel(),
    onBack: (() -> Unit)? = null
) {
    val context = LocalContext.current
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val actionLoading by viewModel.actionLoading.collectAsStateWithLifecycle()
    val userMessage by viewModel.userMessage.collectAsStateWithLifecycle()

    var activeDialogAction by remember { mutableStateOf<Pair<AdminRefundRequest, RefundActionType>?>(null) }
    var selectedFilter by remember { mutableStateOf<String?>(null) } // null = all, "Pending", "Approved", "Rejected"

    LaunchedEffect(userMessage) {
        userMessage?.let {
            Toast.makeText(context, it, Toast.LENGTH_SHORT).show()
            viewModel.clearUserMessage()
        }
    }

    Scaffold(
        topBar = {
            AdminTopBar(
                title = stringResource(R.string.admin_refunds_title),
                onBack = onBack
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .horizontalScroll(rememberScrollState())
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                FilterChip(
                    selected = selectedFilter == null,
                    onClick = {
                        selectedFilter = null
                        viewModel.setStatusFilter(null)
                    },
                    label = { Text(stringResource(R.string.admin_refund_status_all)) }
                )
                FilterChip(
                    selected = selectedFilter == "Pending",
                    onClick = {
                        selectedFilter = "Pending"
                        viewModel.setStatusFilter("Pending")
                    },
                    label = { Text(stringResource(R.string.admin_refund_status_pending)) }
                )
                FilterChip(
                    selected = selectedFilter == "Approved",
                    onClick = {
                        selectedFilter = "Approved"
                        viewModel.setStatusFilter("Approved")
                    },
                    label = { Text(stringResource(R.string.admin_refund_status_approved)) }
                )
                FilterChip(
                    selected = selectedFilter == "Rejected",
                    onClick = {
                        selectedFilter = "Rejected"
                        viewModel.setStatusFilter("Rejected")
                    },
                    label = { Text(stringResource(R.string.admin_refund_status_rejected)) }
                )
            }

            when (val state = uiState) {
                is AdminRefundUiState.Loading -> LoadingContent()
                is AdminRefundUiState.Error -> ErrorContent(
                    message = state.message,
                    onRetry = viewModel::loadRefunds
                )
                is AdminRefundUiState.Empty -> EmptyContent(
                    message = stringResource(R.string.admin_refunds_empty),
                    onRetry = viewModel::loadRefunds
                )
                is AdminRefundUiState.Success -> {
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(horizontal = 16.dp),
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        items(state.refunds, key = { it.id }) { refund ->
                            AdminRefundCard(
                                refund = refund,
                                onApprove = {
                                    activeDialogAction = Pair(refund, RefundActionType.APPROVE)
                                },
                                onReject = {
                                    activeDialogAction = Pair(refund, RefundActionType.REJECT)
                                }
                            )
                        }
                        item { Spacer(modifier = Modifier.height(16.dp)) }
                    }
                }
            }
        }
    }

    activeDialogAction?.let { (refund, action) ->
        RefundActionDialog(
            refund = refund,
            action = action,
            isLoading = actionLoading,
            onDismiss = { if (!actionLoading) activeDialogAction = null },
            onConfirm = { note ->
                if (action == RefundActionType.APPROVE) {
                    viewModel.approveRefund(refund.id, note) {
                        activeDialogAction = null
                    }
                } else {
                    viewModel.rejectRefund(refund.id, note) {
                        activeDialogAction = null
                    }
                }
            }
        )
    }
}

@Composable
private fun AdminRefundCard(
    refund: AdminRefundRequest,
    onApprove: () -> Unit,
    onReject: () -> Unit
) {
    val isPending = refund.status.equals("Pending", ignoreCase = true)
    val isApproved = refund.status.equals("Approved", ignoreCase = true)
    val isRejected = refund.status.equals("Rejected", ignoreCase = true)

    val (statusLabel, statusBg, statusFg) = when {
        isApproved -> Triple("Đã duyệt", MaterialTheme.colorScheme.primaryContainer, MaterialTheme.colorScheme.onPrimaryContainer)
        isRejected -> Triple("Đã từ chối", MaterialTheme.colorScheme.errorContainer, MaterialTheme.colorScheme.onErrorContainer)
        else -> Triple("Chờ xử lý", MaterialTheme.colorScheme.tertiaryContainer, MaterialTheme.colorScheme.onTertiaryContainer)
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Hóa đơn: ${refund.invoiceId}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )

                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(4.dp))
                        .background(statusBg)
                        .padding(horizontal = 8.dp, vertical = 4.dp)
                ) {
                    Text(
                        text = statusLabel,
                        style = MaterialTheme.typography.labelSmall,
                        color = statusFg,
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }

            if (!refund.customerName.isNullOrBlank() || refund.customerId.isNotBlank()) {
                Text(
                    text = "Khách hàng: ${refund.customerName ?: refund.customerId}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            if (!refund.movieTitle.isNullOrBlank()) {
                Text(
                    text = "Phim: ${refund.movieTitle}" +
                        if (!refund.startTime.isNullOrBlank()) " (${refund.showDate ?: ""} ${refund.startTime})" else "",
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium
                )
            }

            Text(
                text = stringResource(R.string.admin_refund_amount, DateTimeUtils.formatCurrency(refund.refundAmount)),
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary
            )

            if (refund.reason.isNotBlank()) {
                Text(
                    text = stringResource(R.string.admin_refund_reason, refund.reason),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            if (!refund.adminNote.isNullOrBlank()) {
                Text(
                    text = "Phản hồi admin: ${refund.adminNote}",
                    style = MaterialTheme.typography.bodySmall,
                    fontWeight = FontWeight.Medium,
                    color = MaterialTheme.colorScheme.secondary
                )
            }

            if (isPending) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 4.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.End)
                ) {
                    OutlinedButton(
                        onClick = onReject,
                        colors = ButtonDefaults.outlinedButtonColors(contentColor = MaterialTheme.colorScheme.error)
                    ) {
                        Text(stringResource(R.string.admin_refund_reject))
                    }
                    Button(onClick = onApprove) {
                        Text(stringResource(R.string.admin_refund_approve))
                    }
                }
            }
        }
    }
}

@Composable
private fun RefundActionDialog(
    refund: AdminRefundRequest,
    action: RefundActionType,
    isLoading: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (note: String?) -> Unit
) {
    var note by remember { mutableStateOf("") }
    val isApprove = action == RefundActionType.APPROVE

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(
                if (isApprove) stringResource(R.string.admin_refund_approve_confirm_title)
                else stringResource(R.string.admin_refund_reject_confirm_title)
            )
        },
        text = {
            Column(
                modifier = Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    if (isApprove) stringResource(R.string.admin_refund_approve_confirm_message)
                    else stringResource(R.string.admin_refund_reject_confirm_message)
                )

                Text(
                    text = "Hóa đơn: ${refund.invoiceId} - ${DateTimeUtils.formatCurrency(refund.refundAmount)}",
                    fontWeight = FontWeight.Bold
                )

                OutlinedTextField(
                    value = note,
                    onValueChange = { note = it },
                    label = { Text(stringResource(R.string.admin_refund_note)) },
                    placeholder = { Text(stringResource(R.string.admin_refund_note_placeholder)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    maxLines = 3
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onConfirm(note) },
                enabled = !isLoading,
                colors = if (isApprove) ButtonDefaults.buttonColors()
                else ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
            ) {
                if (isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        color = MaterialTheme.colorScheme.onPrimary,
                        strokeWidth = 2.dp
                    )
                } else {
                    Text(
                        if (isApprove) stringResource(R.string.admin_refund_approve)
                        else stringResource(R.string.admin_refund_reject)
                    )
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isLoading) {
                Text(stringResource(R.string.cancel))
            }
        }
    )
}
