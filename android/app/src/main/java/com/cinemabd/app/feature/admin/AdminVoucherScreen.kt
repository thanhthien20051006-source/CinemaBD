package com.cinemabd.app.feature.admin

import android.widget.Toast
import androidx.compose.foundation.background
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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SuggestionChip
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
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.feature.admin.model.AdminVoucher
import java.time.LocalDate
import java.time.format.DateTimeFormatter

@Composable
fun AdminVoucherScreen(
    viewModel: AdminVoucherViewModel = hiltViewModel(),
    onBack: (() -> Unit)? = null
) {
    val context = LocalContext.current
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val actionLoading by viewModel.actionLoading.collectAsStateWithLifecycle()
    val userMessage by viewModel.userMessage.collectAsStateWithLifecycle()

    var searchQuery by remember { mutableStateOf("") }
    var showCreateDialog by remember { mutableStateOf(false) }
    var voucherToDelete by remember { mutableStateOf<AdminVoucher?>(null) }

    LaunchedEffect(userMessage) {
        userMessage?.let {
            Toast.makeText(context, it, Toast.LENGTH_SHORT).show()
            viewModel.clearUserMessage()
        }
    }

    Scaffold(
        topBar = {
            AdminTopBar(
                title = stringResource(R.string.admin_vouchers_title),
                onBack = onBack
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = { showCreateDialog = true },
                containerColor = MaterialTheme.colorScheme.primary
            ) {
                Icon(Icons.Filled.Add, contentDescription = stringResource(R.string.admin_voucher_add))
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            OutlinedTextField(
                value = searchQuery,
                onValueChange = {
                    searchQuery = it
                    viewModel.onSearchChange(it)
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text(stringResource(R.string.admin_vouchers_search)) },
                leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
                singleLine = true
            )

            when (val state = uiState) {
                is AdminVoucherUiState.Loading -> LoadingContent()
                is AdminVoucherUiState.Error -> ErrorContent(
                    message = state.message,
                    onRetry = viewModel::loadVouchers
                )
                is AdminVoucherUiState.Empty -> EmptyContent(
                    message = stringResource(R.string.admin_vouchers_empty),
                    onRetry = viewModel::loadVouchers
                )
                is AdminVoucherUiState.Success -> {
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(horizontal = 16.dp),
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        items(state.vouchers, key = { it.id }) { voucher ->
                            AdminVoucherCard(
                                voucher = voucher,
                                onDelete = { voucherToDelete = voucher }
                            )
                        }
                        item { Spacer(modifier = Modifier.height(72.dp)) }
                    }
                }
            }
        }
    }

    if (showCreateDialog) {
        VoucherCreateDialog(
            isLoading = actionLoading,
            onDismiss = { if (!actionLoading) showCreateDialog = false },
            onSave = { code, desc, expireDate, discountVal, discountType, minOrder, maxDiscount ->
                viewModel.createVoucher(
                    code = code,
                    description = desc,
                    expiredAt = expireDate,
                    discountValue = discountVal,
                    discountType = discountType,
                    minOrderAmount = minOrder,
                    maxDiscountAmount = maxDiscount,
                    onSuccess = { showCreateDialog = false }
                )
            }
        )
    }

    voucherToDelete?.let { voucher ->
        AlertDialog(
            onDismissRequest = { if (!actionLoading) voucherToDelete = null },
            title = { Text(stringResource(R.string.admin_voucher_delete_confirm_title)) },
            text = { Text(stringResource(R.string.admin_voucher_delete_confirm_message) + "\n\n${voucher.code}") },
            confirmButton = {
                Button(
                    onClick = {
                        viewModel.deleteVoucher(voucher.id) {
                            voucherToDelete = null
                        }
                    },
                    enabled = !actionLoading
                ) {
                    if (actionLoading) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            color = MaterialTheme.colorScheme.onPrimary,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Text(stringResource(R.string.admin_voucher_delete))
                    }
                }
            },
            dismissButton = {
                OutlinedButton(
                    onClick = { voucherToDelete = null },
                    enabled = !actionLoading
                ) {
                    Text(stringResource(R.string.cancel))
                }
            }
        )
    }
}

@Composable
private fun AdminVoucherCard(
    voucher: AdminVoucher,
    onDelete: () -> Unit
) {
    val isPercent = voucher.discountType.equals("Percent", ignoreCase = true)
    val discountText = if (isPercent) {
        "Giảm ${voucher.discountValue.toInt()}%" +
            if (voucher.maxDiscountAmount > 0) " (Tối đa ${DateTimeUtils.formatCurrency(voucher.maxDiscountAmount)})" else ""
    } else {
        "Giảm ${DateTimeUtils.formatCurrency(voucher.discountValue)}"
    }

    val (statusLabel, statusBg, statusFg) = when {
        !voucher.isActive -> Triple("Đã vô hiệu", Color.LightGray.copy(alpha = 0.3f), Color.DarkGray)
        voucher.isUsed -> Triple("Đã dùng", MaterialTheme.colorScheme.tertiaryContainer, MaterialTheme.colorScheme.onTertiaryContainer)
        voucher.isExpired -> Triple("Hết hạn", MaterialTheme.colorScheme.errorContainer, MaterialTheme.colorScheme.onErrorContainer)
        else -> Triple("Còn hạn", MaterialTheme.colorScheme.primaryContainer, MaterialTheme.colorScheme.onPrimaryContainer)
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
                    text = voucher.code,
                    style = MaterialTheme.typography.titleMedium.copy(fontFamily = FontFamily.Monospace),
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary
                )

                Row(
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
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

                    if (voucher.isActive) {
                        IconButton(onClick = onDelete, modifier = Modifier.size(32.dp)) {
                            Icon(
                                Icons.Filled.Delete,
                                contentDescription = stringResource(R.string.admin_voucher_delete),
                                tint = MaterialTheme.colorScheme.error,
                                modifier = Modifier.size(20.dp)
                            )
                        }
                    }
                }
            }

            Text(
                text = discountText,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Bold
            )

            if (!voucher.description.isNullOrBlank()) {
                Text(
                    text = voucher.description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = "Đơn tối thiểu: ${DateTimeUtils.formatCurrency(voucher.minOrderAmount)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = "HSD: ${voucher.expiredAt.substringBefore('T')}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}

@Composable
private fun VoucherCreateDialog(
    isLoading: Boolean,
    onDismiss: () -> Unit,
    onSave: (
        code: String,
        desc: String?,
        expireDate: String,
        discountVal: Double,
        discountType: String,
        minOrder: Double,
        maxDiscount: Double
    ) -> Unit
) {
    var code by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var discountType by remember { mutableStateOf("Amount") } // "Amount" or "Percent"
    var discountValueText by remember { mutableStateOf("") }
    var minOrderText by remember { mutableStateOf("0") }
    var maxDiscountText by remember { mutableStateOf("0") }
    val defaultExpire = remember { LocalDate.now().plusDays(30).format(DateTimeFormatter.ISO_LOCAL_DATE) }
    var expireDateText by remember { mutableStateOf(defaultExpire) }

    var errorMessage by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(R.string.admin_voucher_add)) },
        text = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                OutlinedTextField(
                    value = code,
                    onValueChange = { code = it.uppercase().filter { ch -> ch.isLetterOrDigit() || ch == '_' } },
                    label = { Text(stringResource(R.string.admin_voucher_code)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    singleLine = true
                )

                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(stringResource(R.string.admin_voucher_desc)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    maxLines = 2
                )

                Text(
                    text = stringResource(R.string.admin_voucher_discount_type),
                    style = MaterialTheme.typography.labelMedium
                )
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        RadioButton(
                            selected = discountType == "Amount",
                            onClick = { discountType = "Amount" },
                            enabled = !isLoading
                        )
                        Text(stringResource(R.string.admin_voucher_type_amount))
                    }
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        RadioButton(
                            selected = discountType == "Percent",
                            onClick = { discountType = "Percent" },
                            enabled = !isLoading
                        )
                        Text(stringResource(R.string.admin_voucher_type_percent))
                    }
                }

                OutlinedTextField(
                    value = discountValueText,
                    onValueChange = { discountValueText = it.filter { ch -> ch.isDigit() } },
                    label = {
                        Text(
                            if (discountType == "Percent") stringResource(R.string.admin_voucher_discount) + " (%)"
                            else stringResource(R.string.admin_voucher_discount) + " (VNĐ)"
                        )
                    },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    singleLine = true
                )

                if (discountType == "Percent") {
                    OutlinedTextField(
                        value = maxDiscountText,
                        onValueChange = { maxDiscountText = it.filter { ch -> ch.isDigit() } },
                        label = { Text(stringResource(R.string.admin_voucher_max_discount)) },
                        modifier = Modifier.fillMaxWidth(),
                        enabled = !isLoading,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        singleLine = true
                    )
                }

                OutlinedTextField(
                    value = minOrderText,
                    onValueChange = { minOrderText = it.filter { ch -> ch.isDigit() } },
                    label = { Text(stringResource(R.string.admin_voucher_min_order)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    singleLine = true
                )

                OutlinedTextField(
                    value = expireDateText,
                    onValueChange = { expireDateText = it },
                    label = { Text(stringResource(R.string.admin_voucher_expire_date)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    singleLine = true
                )

                errorMessage?.let {
                    Text(
                        text = it,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    val discountVal = discountValueText.toDoubleOrNull()
                    val minOrder = minOrderText.toDoubleOrNull() ?: 0.0
                    val maxDiscount = maxDiscountText.toDoubleOrNull() ?: 0.0
                    when {
                        code.isBlank() -> errorMessage = "Vui lòng nhập mã voucher."
                        discountVal == null || discountVal <= 0.0 -> errorMessage = "Vui lòng nhập mức giảm hợp lệ."
                        discountType == "Percent" && discountVal > 100.0 -> errorMessage = "Phần trăm giảm không được vượt quá 100%."
                        expireDateText.isBlank() -> errorMessage = "Vui lòng nhập ngày hết hạn."
                        else -> {
                            errorMessage = null
                            onSave(code, description, expireDateText, discountVal, discountType, minOrder, maxDiscount)
                        }
                    }
                },
                enabled = !isLoading
            ) {
                if (isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        color = MaterialTheme.colorScheme.onPrimary,
                        strokeWidth = 2.dp
                    )
                } else {
                    Text(stringResource(R.string.save))
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
