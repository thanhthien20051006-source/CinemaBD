package com.cinemabd.app.feature.admin

import android.widget.Toast
import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.material.icons.filled.Edit
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
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.compose.AsyncImage
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.core.utils.formatImageUrl
import com.cinemabd.app.feature.admin.model.AdminCombo

@Composable
fun AdminComboScreen(
    viewModel: AdminComboViewModel = hiltViewModel(),
    onBack: (() -> Unit)? = null
) {
    val context = LocalContext.current
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val actionLoading by viewModel.actionLoading.collectAsStateWithLifecycle()
    val userMessage by viewModel.userMessage.collectAsStateWithLifecycle()

    var showUpsertDialog by remember { mutableStateOf(false) }
    var editingCombo by remember { mutableStateOf<AdminCombo?>(null) }
    var comboToDelete by remember { mutableStateOf<AdminCombo?>(null) }

    LaunchedEffect(userMessage) {
        userMessage?.let {
            Toast.makeText(context, it, Toast.LENGTH_SHORT).show()
            viewModel.clearUserMessage()
        }
    }

    Scaffold(
        topBar = {
            AdminTopBar(
                title = stringResource(R.string.admin_combos_title),
                onBack = onBack
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = {
                    editingCombo = null
                    showUpsertDialog = true
                },
                containerColor = MaterialTheme.colorScheme.primary
            ) {
                Icon(Icons.Filled.Add, contentDescription = stringResource(R.string.admin_combo_add))
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when (val state = uiState) {
                is AdminComboUiState.Loading -> LoadingContent()
                is AdminComboUiState.Error -> ErrorContent(
                    message = state.message,
                    onRetry = viewModel::loadCombos
                )
                is AdminComboUiState.Empty -> EmptyContent(
                    message = stringResource(R.string.admin_combos_empty),
                    onRetry = viewModel::loadCombos
                )
                is AdminComboUiState.Success -> {
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(state.combos, key = { it.id }) { combo ->
                            AdminComboCard(
                                combo = combo,
                                onEdit = {
                                    editingCombo = combo
                                    showUpsertDialog = true
                                },
                                onDelete = {
                                    comboToDelete = combo
                                }
                            )
                        }
                    }
                }
            }
        }
    }

    if (showUpsertDialog) {
        ComboUpsertDialog(
            combo = editingCombo,
            isLoading = actionLoading,
            onDismiss = { if (!actionLoading) showUpsertDialog = false },
            onSave = { id, name, price, desc, img ->
                viewModel.saveCombo(
                    id = id,
                    name = name,
                    price = price,
                    description = desc,
                    imageUrl = img,
                    isEdit = editingCombo != null,
                    onSuccess = { showUpsertDialog = false }
                )
            }
        )
    }

    comboToDelete?.let { combo ->
        AlertDialog(
            onDismissRequest = { if (!actionLoading) comboToDelete = null },
            title = { Text(stringResource(R.string.admin_combo_delete_confirm_title)) },
            text = { Text(stringResource(R.string.admin_combo_delete_confirm_message) + "\n\n${combo.name} (${combo.id})") },
            confirmButton = {
                Button(
                    onClick = {
                        viewModel.deleteCombo(combo.id) {
                            comboToDelete = null
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
                        Text(stringResource(R.string.admin_combo_delete))
                    }
                }
            },
            dismissButton = {
                OutlinedButton(
                    onClick = { comboToDelete = null },
                    enabled = !actionLoading
                ) {
                    Text(stringResource(R.string.cancel))
                }
            }
        )
    }
}

@Composable
private fun AdminComboCard(
    combo: AdminCombo,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            val formattedImg = formatImageUrl(combo.imageUrl)
            if (!formattedImg.isNullOrBlank()) {
                AsyncImage(
                    model = formattedImg,
                    contentDescription = combo.name,
                    modifier = Modifier
                        .size(72.dp)
                        .clip(RoundedCornerShape(8.dp)),
                    contentScale = ContentScale.Crop
                )
                Spacer(modifier = Modifier.width(14.dp))
            }

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                Text(
                    text = combo.name,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "Mã: ${combo.id}",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.primary
                )
                Text(
                    text = DateTimeUtils.formatCurrency(combo.price),
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.SemiBold,
                    color = MaterialTheme.colorScheme.secondary
                )
                if (!combo.description.isNullOrBlank()) {
                    Text(
                        text = combo.description,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 2
                    )
                }
            }

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                IconButton(onClick = onEdit) {
                    Icon(
                        Icons.Filled.Edit,
                        contentDescription = stringResource(R.string.admin_combo_edit),
                        tint = MaterialTheme.colorScheme.primary
                    )
                }
                IconButton(onClick = onDelete) {
                    Icon(
                        Icons.Filled.Delete,
                        contentDescription = stringResource(R.string.admin_combo_delete),
                        tint = MaterialTheme.colorScheme.error
                    )
                }
            }
        }
    }
}

@Composable
private fun ComboUpsertDialog(
    combo: AdminCombo?,
    isLoading: Boolean,
    onDismiss: () -> Unit,
    onSave: (id: String, name: String, price: Double, desc: String?, img: String?) -> Unit
) {
    val isEdit = combo != null
    var id by remember { mutableStateOf(combo?.id.orEmpty()) }
    var name by remember { mutableStateOf(combo?.name.orEmpty()) }
    var priceText by remember { mutableStateOf(combo?.price?.toLong()?.toString().orEmpty()) }
    var description by remember { mutableStateOf(combo?.description.orEmpty()) }
    var imageUrl by remember { mutableStateOf(combo?.imageUrl.orEmpty()) }

    var errorMessage by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(if (isEdit) stringResource(R.string.admin_combo_edit) else stringResource(R.string.admin_combo_add))
        },
        text = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                OutlinedTextField(
                    value = id,
                    onValueChange = { if (!isEdit) id = it },
                    label = { Text(stringResource(R.string.admin_combo_id)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isEdit && !isLoading,
                    singleLine = true
                )

                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text(stringResource(R.string.admin_combo_name)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    singleLine = true
                )

                OutlinedTextField(
                    value = priceText,
                    onValueChange = { priceText = it.filter { ch -> ch.isDigit() } },
                    label = { Text(stringResource(R.string.admin_combo_price)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    singleLine = true
                )

                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(stringResource(R.string.admin_combo_desc)) },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    maxLines = 3
                )

                OutlinedTextField(
                    value = imageUrl,
                    onValueChange = { imageUrl = it },
                    label = { Text(stringResource(R.string.admin_combo_image)) },
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
                    val price = priceText.toDoubleOrNull()
                    when {
                        id.isBlank() -> errorMessage = "Vui lòng nhập mã combo."
                        name.isBlank() -> errorMessage = "Vui lòng nhập tên combo."
                        price == null || price <= 0.0 -> errorMessage = "Vui lòng nhập giá bán hợp lệ (> 0 VNĐ)."
                        else -> {
                            errorMessage = null
                            onSave(id, name, price, description, imageUrl)
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
                    Text(stringResource(R.string.admin_combo_save))
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
