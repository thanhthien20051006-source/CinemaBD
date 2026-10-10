package com.cinemabd.app.feature.profile

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.cinemabd.app.R
import com.cinemabd.app.core.network.model.CustomerProfileResponse
import com.cinemabd.app.core.utils.DateTimeUtils

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProfileScreen(
    viewModel: ProfileViewModel = hiltViewModel(),
    onLogout: () -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(uiState.isLoggedOut) {
        if (uiState.isLoggedOut) {
            onLogout()
        }
    }

    LaunchedEffect(uiState.successMessage) {
        uiState.successMessage?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.clearMessages()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = stringResource(id = R.string.profile_title),
                        fontWeight = FontWeight.Bold
                    )
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primary,
                    titleContentColor = MaterialTheme.colorScheme.onPrimary
                )
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            when {
                uiState.isLoading -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }
                uiState.errorMessage != null && uiState.profile == null -> {
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.Center
                    ) {
                        Text(
                            text = uiState.errorMessage ?: stringResource(id = R.string.error),
                            style = MaterialTheme.typography.bodyLarge,
                            color = MaterialTheme.colorScheme.error
                        )
                        Spacer(modifier = Modifier.height(16.dp))
                        Button(onClick = { viewModel.loadProfile() }) {
                            Text(stringResource(id = R.string.retry))
                        }
                    }
                }
                else -> {
                    val scrollState = rememberScrollState()
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .verticalScroll(scrollState)
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        // Error banner if any error occurs while keeping existing profile
                        if (uiState.errorMessage != null) {
                            Card(
                                colors = CardDefaults.cardColors(
                                    containerColor = MaterialTheme.colorScheme.errorContainer
                                ),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(12.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Icon(
                                        imageVector = Icons.Default.Info,
                                        contentDescription = null,
                                        tint = MaterialTheme.colorScheme.error
                                    )
                                    Spacer(modifier = Modifier.width(8.dp))
                                    Text(
                                        text = uiState.errorMessage.orEmpty(),
                                        style = MaterialTheme.typography.bodyMedium,
                                        color = MaterialTheme.colorScheme.onErrorContainer
                                    )
                                }
                            }
                        }

                        // User Overview Card
                        uiState.profile?.let { profile ->
                            ProfileHeaderCard(
                                fullName = profile.fullName,
                                username = profile.username,
                                totalSpent = profile.totalSpent,
                                loyaltyPoints = uiState.loyaltyPoints ?: 0
                            )
                        }

                        // Profile Info Section (View or Edit)
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.surface
                            ),
                            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                        ) {
                            Column(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(16.dp),
                                verticalArrangement = Arrangement.spacedBy(12.dp)
                            ) {
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = stringResource(id = R.string.profile_info_title),
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold
                                    )
                                    if (!uiState.isEditMode) {
                                        TextButton(onClick = { viewModel.startEdit() }) {
                                            Icon(
                                                imageVector = Icons.Default.Edit,
                                                contentDescription = null,
                                                modifier = Modifier.size(18.dp)
                                            )
                                            Spacer(modifier = Modifier.width(4.dp))
                                            Text(stringResource(id = R.string.profile_edit_btn))
                                        }
                                    }
                                }

                                if (uiState.isEditMode) {
                                    ProfileEditForm(
                                        uiState = uiState,
                                        onFullNameChange = viewModel::onFullNameChange,
                                        onEmailChange = viewModel::onEmailChange,
                                        onPhoneChange = viewModel::onPhoneNumberChange,
                                        onBirthDateChange = viewModel::onBirthDateChange,
                                        onSave = viewModel::saveProfile,
                                        onCancel = viewModel::cancelEdit
                                    )
                                } else {
                                    uiState.profile?.let { profile ->
                                        ProfileInfoView(
                                            profile = profile,
                                            loyaltyPoints = uiState.loyaltyPoints
                                        )
                                    }
                                }
                            }
                        }

                        // App Settings Section (Dark Theme Toggle)
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.surface
                            ),
                            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                        ) {
                            Column(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(16.dp)
                            ) {
                                Text(
                                    text = stringResource(id = R.string.profile_settings_title),
                                    style = MaterialTheme.typography.titleMedium,
                                    fontWeight = FontWeight.Bold,
                                    modifier = Modifier.padding(bottom = 8.dp)
                                )
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Row(
                                        verticalAlignment = Alignment.CenterVertically,
                                        modifier = Modifier.weight(1f)
                                    ) {
                                        Icon(
                                            imageVector = Icons.Default.Settings,
                                            contentDescription = null,
                                            tint = MaterialTheme.colorScheme.primary,
                                            modifier = Modifier.size(24.dp)
                                        )
                                        Spacer(modifier = Modifier.width(12.dp))
                                        Column {
                                            Text(
                                                text = stringResource(id = R.string.profile_dark_mode),
                                                style = MaterialTheme.typography.bodyLarge,
                                                fontWeight = FontWeight.Medium
                                            )
                                            Text(
                                                text = stringResource(id = R.string.profile_dark_mode_desc),
                                                style = MaterialTheme.typography.bodySmall,
                                                color = MaterialTheme.colorScheme.onSurfaceVariant
                                            )
                                        }
                                    }
                                    Switch(
                                        checked = uiState.isDarkMode,
                                        onCheckedChange = { viewModel.setDarkMode(it) }
                                    )
                                }
                            }
                        }

                        // Account Actions Section (Logout)
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.surface
                            ),
                            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                        ) {
                            Column(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(16.dp),
                                verticalArrangement = Arrangement.spacedBy(12.dp)
                            ) {
                                Text(
                                    text = stringResource(id = R.string.profile_account_actions_title),
                                    style = MaterialTheme.typography.titleMedium,
                                    fontWeight = FontWeight.Bold
                                )

                                Button(
                                    onClick = { viewModel.openLogoutDialog() },
                                    modifier = Modifier.fillMaxWidth(),
                                    colors = ButtonDefaults.buttonColors(
                                        containerColor = MaterialTheme.colorScheme.error,
                                        contentColor = MaterialTheme.colorScheme.onError
                                    ),
                                    shape = RoundedCornerShape(8.dp)
                                ) {
                                    Icon(
                                        imageVector = Icons.AutoMirrored.Filled.ExitToApp,
                                        contentDescription = null,
                                        modifier = Modifier.size(20.dp)
                                    )
                                    Spacer(modifier = Modifier.width(8.dp))
                                    Text(
                                        text = stringResource(id = R.string.profile_logout_btn),
                                        fontWeight = FontWeight.Bold
                                    )
                                }
                            }
                        }

                        Spacer(modifier = Modifier.height(16.dp))
                    }
                }
            }
        }
    }

    if (uiState.showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissLogoutDialog() },
            title = {
                Text(
                    text = stringResource(id = R.string.profile_logout_dialog_title),
                    fontWeight = FontWeight.Bold
                )
            },
            text = {
                Text(text = stringResource(id = R.string.profile_logout_dialog_message))
            },
            confirmButton = {
                Button(
                    onClick = { viewModel.logout() },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Text(stringResource(id = R.string.profile_logout_confirm))
                }
            },
            dismissButton = {
                OutlinedButton(onClick = { viewModel.dismissLogoutDialog() }) {
                    Text(stringResource(id = R.string.profile_logout_cancel))
                }
            }
        )
    }
}

@Composable
private fun ProfileHeaderCard(
    fullName: String,
    username: String,
    totalSpent: Double,
    loyaltyPoints: Int
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.primaryContainer
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Box(
                modifier = Modifier
                    .size(72.dp)
                    .clip(CircleShape)
                    .background(MaterialTheme.colorScheme.primary),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Default.Person,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.onPrimary,
                    modifier = Modifier.size(44.dp)
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = fullName.ifBlank { username },
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onPrimaryContainer
            )

            Text(
                text = "@$username",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.7f)
            )

            Spacer(modifier = Modifier.height(16.dp))
            HorizontalDivider(color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.2f))
            Spacer(modifier = Modifier.height(16.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        text = stringResource(id = R.string.profile_total_spent),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.7f)
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = DateTimeUtils.formatCurrency(totalSpent),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                }

                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        text = stringResource(id = R.string.profile_loyalty_points),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.7f)
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = stringResource(id = R.string.profile_points_unit, loyaltyPoints),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.secondary
                    )
                }
            }
        }
    }
}

@Composable
private fun ProfileInfoView(
    profile: CustomerProfileResponse,
    loyaltyPoints: Int?
) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        InfoRow(
            icon = Icons.Default.Person,
            label = stringResource(id = R.string.profile_full_name),
            value = profile.fullName.ifBlank { stringResource(id = R.string.profile_not_updated) }
        )
        InfoRow(
            icon = Icons.Default.AccountBox,
            label = stringResource(id = R.string.profile_username),
            value = profile.username
        )
        InfoRow(
            icon = Icons.Default.Email,
            label = stringResource(id = R.string.profile_email),
            value = profile.email?.takeIf { it.isNotBlank() } ?: stringResource(id = R.string.profile_not_updated)
        )
        InfoRow(
            icon = Icons.Default.Phone,
            label = stringResource(id = R.string.profile_phone),
            value = profile.phoneNumber?.takeIf { it.isNotBlank() } ?: stringResource(id = R.string.profile_not_updated)
        )
        InfoRow(
            icon = Icons.Default.DateRange,
            label = stringResource(id = R.string.profile_birth_date),
            value = formatBirthDate(profile.birthDate) ?: stringResource(id = R.string.profile_not_updated)
        )
        if (loyaltyPoints != null) {
            InfoRow(
                icon = Icons.Default.Star,
                label = stringResource(id = R.string.profile_loyalty_points),
                value = stringResource(id = R.string.profile_points_unit, loyaltyPoints)
            )
        }
    }
}

@Composable
private fun InfoRow(
    icon: ImageVector,
    label: String,
    value: String
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(20.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = value,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Medium
            )
        }
    }
}

@Composable
private fun ProfileEditForm(
    uiState: ProfileUiState,
    onFullNameChange: (String) -> Unit,
    onEmailChange: (String) -> Unit,
    onPhoneChange: (String) -> Unit,
    onBirthDateChange: (String) -> Unit,
    onSave: () -> Unit,
    onCancel: () -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        // Full Name
        OutlinedTextField(
            value = uiState.editFullName,
            onValueChange = onFullNameChange,
            label = { Text(stringResource(id = R.string.profile_full_name)) },
            modifier = Modifier.fillMaxWidth(),
            isError = uiState.fullNameError != null,
            supportingText = {
                uiState.fullNameError?.let {
                    Text(text = it, color = MaterialTheme.colorScheme.error)
                }
            },
            singleLine = true,
            enabled = !uiState.isSaving
        )

        // Username (Read-only)
        OutlinedTextField(
            value = uiState.profile?.username.orEmpty(),
            onValueChange = {},
            label = { Text(stringResource(id = R.string.profile_username)) },
            modifier = Modifier.fillMaxWidth(),
            enabled = false,
            singleLine = true
        )

        // Email
        OutlinedTextField(
            value = uiState.editEmail,
            onValueChange = onEmailChange,
            label = { Text(stringResource(id = R.string.profile_email)) },
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
            isError = uiState.emailError != null,
            supportingText = {
                uiState.emailError?.let {
                    Text(text = it, color = MaterialTheme.colorScheme.error)
                }
            },
            singleLine = true,
            enabled = !uiState.isSaving
        )

        // Phone Number
        OutlinedTextField(
            value = uiState.editPhoneNumber,
            onValueChange = onPhoneChange,
            label = { Text(stringResource(id = R.string.profile_phone)) },
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
            isError = uiState.phoneError != null,
            supportingText = {
                uiState.phoneError?.let {
                    Text(text = it, color = MaterialTheme.colorScheme.error)
                }
            },
            singleLine = true,
            enabled = !uiState.isSaving
        )

        // Birth Date
        OutlinedTextField(
            value = uiState.editBirthDate,
            onValueChange = onBirthDateChange,
            label = { Text(stringResource(id = R.string.profile_birth_date)) },
            placeholder = { Text("YYYY-MM-DD") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            enabled = !uiState.isSaving
        )

        Spacer(modifier = Modifier.height(4.dp))

        // Buttons Row
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            OutlinedButton(
                onClick = onCancel,
                modifier = Modifier.weight(1f),
                enabled = !uiState.isSaving
            ) {
                Text(stringResource(id = R.string.profile_cancel_btn))
            }

            Button(
                onClick = onSave,
                modifier = Modifier.weight(1f),
                enabled = !uiState.isSaving
            ) {
                if (uiState.isSaving) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(18.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                }
                Text(stringResource(id = R.string.profile_save_btn))
            }
        }
    }
}

private fun formatBirthDate(rawDate: String?): String? {
    if (rawDate.isNullOrBlank()) return null
    return try {
        val clean = rawDate.substringBefore('T').trim()
        val parts = clean.split('-')
        if (parts.size == 3) {
            "${parts[2]}/${parts[1]}/${parts[0]}"
        } else {
            clean
        }
    } catch (e: Exception) {
        rawDate
    }
}
