package com.cinemabd.app.feature.admin

import android.app.DatePickerDialog
import android.widget.Toast
import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import kotlinx.coroutines.launch
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.navigation.Routes
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AdminDashboardScreen(
    navController: NavController? = null,
    viewModel: AdminDashboardViewModel = hiltViewModel(),
    onNavigateToScanner: () -> Unit,
    onNavigateToShowtimeDetail: (showtimeId: String, showDate: String) -> Unit,
    onLogout: (() -> Unit)? = null,
    onExitAdmin: (() -> Unit)? = null
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()
    var showLogoutDialog by remember { mutableStateOf(false) }

    val currentBackStackEntry = navController?.currentBackStackEntry
    LaunchedEffect(currentBackStackEntry) {
        currentBackStackEntry?.savedStateHandle
            ?.getStateFlow("ticket_scanned", false)
            ?.collect { isScanned ->
                if (isScanned) {
                    viewModel.refresh(silent = true)
                    currentBackStackEntry.savedStateHandle["ticket_scanned"] = false
                }
            }
    }

    LaunchedEffect(uiState.actionMessage) {
        uiState.actionMessage?.let {
            Toast.makeText(context, it, Toast.LENGTH_SHORT).show()
            viewModel.clearActionMessage()
        }
    }

    val datePickerDialog = remember(uiState.selectedDate) {
        DatePickerDialog(
            context,
            { _, year, month, dayOfMonth ->
                val pickedDate = LocalDate.of(year, month + 1, dayOfMonth)
                viewModel.onDateSelected(pickedDate)
            },
            uiState.selectedDate.year,
            uiState.selectedDate.monthValue - 1,
            uiState.selectedDate.dayOfMonth
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(hostState = snackbarHostState) },
        topBar = {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column {
                    Text(
                        text = stringResource(R.string.admin_dashboard_title),
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onBackground
                    )
                    Text(
                        text = uiState.selectedDate.format(
                            DateTimeFormatter.ofPattern("EEEE, dd/MM/yyyy", Locale.forLanguageTag("vi-VN"))
                        ).replaceFirstChar { it.uppercase() },
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    IconButton(onClick = viewModel::onRefresh) {
                        Icon(
                            imageVector = Icons.Default.Refresh,
                            contentDescription = stringResource(R.string.retry),
                            tint = MaterialTheme.colorScheme.primary
                        )
                    }
                    IconButton(onClick = { showLogoutDialog = true }) {
                        Icon(
                            imageVector = Icons.AutoMirrored.Filled.ExitToApp,
                            contentDescription = stringResource(R.string.profile_logout_btn),
                            tint = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
        }
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState.isRefreshing,
            onRefresh = viewModel::onRefresh,
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // 1. KPI Summary Cards
                item {
                    KpiSummarySection(
                        stats = uiState.todayStats,
                        isToday = uiState.selectedDate == LocalDate.now()
                    )
                }

                // 2. Quick Actions
                item {
                    QuickActionsSection(
                        onOpenScanner = onNavigateToScanner,
                        onAddShowtime = viewModel::openAddShowtimeDialog
                    )
                }

                // 3. Date Filter Chips
                item {
                    DateFilterSection(
                        selectedDate = uiState.selectedDate,
                        onDateSelected = viewModel::onDateSelected,
                        onOpenDatePicker = { datePickerDialog.show() }
                    )
                }

                // 4. Showtimes Header
                item {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = stringResource(
                                R.string.admin_dashboard_showtimes_header,
                                uiState.showtimes.size
                            ),
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }

                // 5. Showtimes List or States
                if (uiState.isLoading && !uiState.isRefreshing) {
                    item {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(200.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator()
                        }
                    }
                } else if (uiState.errorMessage != null) {
                    item {
                        ErrorContent(
                            message = uiState.errorMessage ?: "",
                            onRetry = { viewModel.loadDashboardData(uiState.selectedDate) }
                        )
                    }
                } else if (uiState.showtimes.isEmpty()) {
                    item {
                        val dateFormatted = uiState.selectedDate.format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
                        EmptyContent(
                            message = stringResource(R.string.admin_dashboard_showtimes_empty, dateFormatted),
                            onRetry = { viewModel.loadDashboardData(uiState.selectedDate) }
                        )
                    }
                } else {
                    items(uiState.showtimes, key = { it.id }) { showtime ->
                        DashboardShowtimeCard(
                            item = showtime,
                            onClick = {
                                onNavigateToShowtimeDetail(
                                    showtime.id,
                                    uiState.selectedDate.format(DateTimeFormatter.ISO_LOCAL_DATE)
                                )
                            },
                            onDeleteClick = { viewModel.onDeleteShowtimeClick(showtime) }
                        )
                    }
                }
            }
        }
    }

    // Add Showtime Dialog / BottomSheet
    if (uiState.showAddDialog) {
        val successMsg = stringResource(R.string.admin_add_showtime_success)
        AddShowtimeDialog(
            initialDate = uiState.selectedDate,
            onDismiss = viewModel::dismissAddShowtimeDialog,
            onSuccess = {
                viewModel.dismissAddShowtimeDialog()
                viewModel.loadDashboardData(uiState.selectedDate, isRefresh = true)
                scope.launch {
                    snackbarHostState.showSnackbar(successMsg)
                }
            }
        )
    }

    // Delete Confirmation Dialog
    uiState.showtimeToDelete?.let { target ->
        AlertDialog(
            onDismissRequest = viewModel::dismissDeleteDialog,
            title = { Text(stringResource(R.string.admin_dashboard_delete_confirm_title)) },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(stringResource(R.string.admin_dashboard_delete_confirm_message))
                    Text(
                        text = "${target.movieTitle} - ${target.roomName} (${target.startTime})",
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = viewModel::confirmDeleteShowtime,
                    enabled = !uiState.isDeletingShowtime,
                    colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
                ) {
                    if (uiState.isDeletingShowtime) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            color = MaterialTheme.colorScheme.onError,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Text(stringResource(R.string.admin_dashboard_delete_btn))
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = viewModel::dismissDeleteDialog,
                    enabled = !uiState.isDeletingShowtime
                ) {
                    Text(stringResource(R.string.profile_logout_cancel))
                }
            }
        )
    }

    // Logout Confirmation Dialog
    if (showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutDialog = false },
            title = { Text(stringResource(R.string.profile_logout_dialog_title)) },
            text = { Text(stringResource(R.string.profile_logout_dialog_message)) },
            confirmButton = {
                Button(
                    onClick = {
                        showLogoutDialog = false
                        onLogout?.invoke() ?: onExitAdmin?.invoke()
                    },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Text(stringResource(R.string.profile_logout_confirm))
                }
            },
            dismissButton = {
                TextButton(onClick = { showLogoutDialog = false }) {
                    Text(stringResource(R.string.profile_logout_cancel))
                }
            }
        )
    }
}

@Composable
private fun KpiSummarySection(stats: DailyStats, isToday: Boolean) {
    Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        // Metric 1: Vé đã quét hôm nay
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = if (isToday) stringResource(R.string.admin_dashboard_tickets_scanned) else "Vé đã quét trong ngày",
                        style = MaterialTheme.typography.titleSmall,
                        fontWeight = FontWeight.Medium,
                        color = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                    Text(
                        text = "${stats.checkedInTickets}/${stats.totalTicketsSold} vé",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                }

                LinearProgressIndicator(
                    progress = { stats.checkInRate },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(8.dp),
                    color = MaterialTheme.colorScheme.primary,
                    trackColor = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.2f),
                    strokeCap = StrokeCap.Round
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        text = "Tiến độ soát vé: ${(stats.checkInRate * 100).toInt()}%",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.8f)
                    )
                    Text(
                        text = "Chưa soát: ${stats.uncheckedTickets} vé",
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Medium,
                        color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.8f)
                    )
                }
            }
        }

        // Row of 2 Cards: Suất chiếu & Doanh thu
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            // Card 2: Suất chiếu
            Card(
                modifier = Modifier.weight(1f),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.secondaryContainer)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Text(
                        text = if (isToday) stringResource(R.string.admin_dashboard_showtimes_today) else "Suất chiếu ngày này",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSecondaryContainer
                    )
                    Text(
                        text = "${stats.showtimeCount}",
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onSecondaryContainer
                    )
                    Text(
                        text = "suất vận hành",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.7f)
                    )
                }
            }

            // Card 3: Doanh thu
            Card(
                modifier = Modifier.weight(1f),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.tertiaryContainer)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Text(
                        text = if (isToday) stringResource(R.string.admin_dashboard_revenue_today) else "Doanh thu ước tính",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onTertiaryContainer
                    )
                    Text(
                        text = DateTimeUtils.formatCurrency(stats.estimatedRevenue),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onTertiaryContainer,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        text = "từ vé đã bán",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onTertiaryContainer.copy(alpha = 0.7f)
                    )
                }
            }
        }
    }
}

@Composable
private fun QuickActionsSection(
    onOpenScanner: () -> Unit,
    onAddShowtime: () -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        // Nút lớn nổi bật: Mở máy quét soát vé
        Button(
            onClick = onOpenScanner,
            modifier = Modifier
                .fillMaxWidth()
                .height(52.dp),
            shape = RoundedCornerShape(14.dp),
            colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary)
        ) {
            Icon(
                painter = painterResource(id = R.drawable.ic_camera),
                contentDescription = null,
                modifier = Modifier.size(24.dp)
            )
            Spacer(modifier = Modifier.width(10.dp))
            Text(
                text = stringResource(R.string.admin_dashboard_open_scanner),
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
        }

        // Nút: Thêm suất chiếu mới
        OutlinedButton(
            onClick = onAddShowtime,
            modifier = Modifier
                .fillMaxWidth()
                .height(48.dp),
            shape = RoundedCornerShape(14.dp)
        ) {
            Icon(
                imageVector = Icons.Default.Add,
                contentDescription = null,
                modifier = Modifier.size(20.dp)
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text(
                text = stringResource(R.string.admin_dashboard_add_showtime),
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.SemiBold
            )
        }
    }
}

@Composable
private fun DateFilterSection(
    selectedDate: LocalDate,
    onDateSelected: (LocalDate) -> Unit,
    onOpenDatePicker: () -> Unit
) {
    val today = remember { LocalDate.now() }
    val tomorrow = remember { LocalDate.now().plusDays(1) }

    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        FilterChip(
            selected = selectedDate == today,
            onClick = { onDateSelected(today) },
            label = { Text(stringResource(R.string.admin_dashboard_date_today)) },
            shape = RoundedCornerShape(10.dp)
        )

        FilterChip(
            selected = selectedDate == tomorrow,
            onClick = { onDateSelected(tomorrow) },
            label = { Text(stringResource(R.string.admin_dashboard_date_tomorrow)) },
            shape = RoundedCornerShape(10.dp)
        )

        val isCustomDate = selectedDate != today && selectedDate != tomorrow
        FilterChip(
            selected = isCustomDate,
            onClick = onOpenDatePicker,
            label = {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        imageVector = Icons.Default.DateRange,
                        contentDescription = null,
                        modifier = Modifier.size(16.dp)
                    )
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(
                        if (isCustomDate) {
                            selectedDate.format(DateTimeFormatter.ofPattern("dd/MM"))
                        } else {
                            stringResource(R.string.admin_dashboard_date_other)
                        }
                    )
                }
            },
            shape = RoundedCornerShape(10.dp)
        )
    }
}

@Composable
private fun DashboardShowtimeCard(
    item: AdminDashboardShowtimeItem,
    onClick: () -> Unit,
    onDeleteClick: () -> Unit
) {
    var menuExpanded by remember { mutableStateOf(false) }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            // Top row: Giờ chiếu lớn + Status badge + Menu
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "${item.startTime} - ${item.endTime}",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.ExtraBold,
                        color = MaterialTheme.colorScheme.onSurface
                    )
                }

                Row(verticalAlignment = Alignment.CenterVertically) {
                    ShowtimeStatusBadge(status = item.status)

                    Box {
                        IconButton(onClick = { menuExpanded = true }) {
                            Icon(Icons.Default.MoreVert, contentDescription = null)
                        }

                        DropdownMenu(
                            expanded = menuExpanded,
                            onDismissRequest = { menuExpanded = false }
                        ) {
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.admin_dashboard_view_detail)) },
                                onClick = {
                                    menuExpanded = false
                                    onClick()
                                }
                            )
                            if (item.canDelete) {
                                DropdownMenuItem(
                                    text = {
                                        Text(
                                            stringResource(R.string.admin_dashboard_delete_showtime),
                                            color = MaterialTheme.colorScheme.error
                                        )
                                    },
                                    onClick = {
                                        menuExpanded = false
                                        onDeleteClick()
                                    }
                                )
                            }
                        }
                    }
                }
            }

            // Movie title & Room
            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(
                    text = item.movieTitle,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = item.roomName,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.primary,
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(
                        text = "•",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = DateTimeUtils.formatCurrency(item.ticketPrice),
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            // Seat Fill Rate Progress Bar
            val fillPercent = (item.fillRatio * 100).toInt()
            val progressColor by animateColorAsState(
                targetValue = when {
                    item.fillRatio > 0.85f -> Color(0xFFD32F2F) // Đỏ: cháy vé
                    item.fillRatio > 0.50f -> Color(0xFFF57C00) // Vàng/cam: gần kín
                    else -> Color(0xFF2E7D32)                  // Xanh: còn nhiều chỗ
                },
                label = "fillProgressColor"
            )

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        text = stringResource(
                            R.string.admin_dashboard_fill_rate,
                            item.soldSeats,
                            item.totalSeats,
                            fillPercent
                        ),
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Medium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = stringResource(R.string.admin_dashboard_checked_in_count, item.checkedInSeats),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                LinearProgressIndicator(
                    progress = { item.fillRatio },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(6.dp),
                    color = progressColor,
                    trackColor = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.1f),
                    strokeCap = StrokeCap.Round
                )
            }
        }
    }
}

@Composable
private fun ShowtimeStatusBadge(status: ShowtimeDisplayStatus) {
    val (textRes, bgColor, textColor) = when (status) {
        ShowtimeDisplayStatus.UPCOMING -> Triple(
            R.string.admin_dashboard_status_upcoming,
            Color(0xFFE3F2FD),
            Color(0xFF1565C0)
        )
        ShowtimeDisplayStatus.ONGOING -> Triple(
            R.string.admin_dashboard_status_ongoing,
            Color(0xFFE8F5E9),
            Color(0xFF2E7D32)
        )
        ShowtimeDisplayStatus.ENDED -> Triple(
            R.string.admin_dashboard_status_ended,
            Color(0xFFF5F5F5),
            Color(0xFF616161)
        )
        ShowtimeDisplayStatus.CANCELLED -> Triple(
            R.string.admin_dashboard_status_cancelled,
            Color(0xFFFFEBEE),
            Color(0xFFC62828)
        )
    }

    Surface(
        shape = RoundedCornerShape(8.dp),
        color = bgColor
    ) {
        Text(
            text = stringResource(textRes),
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold,
            color = textColor
        )
    }
}
