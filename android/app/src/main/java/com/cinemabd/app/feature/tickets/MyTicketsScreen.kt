package com.cinemabd.app.feature.tickets

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Outline
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import com.cinemabd.app.R
import com.cinemabd.app.core.database.entity.TicketEntity
import com.cinemabd.app.core.utils.DateTimeUtils

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyTicketsScreen(
    viewModel: MyTicketsViewModel = hiltViewModel(),
    onNavigateToHome: () -> Unit = {},
    onTicketDetail: (String) -> Unit = {}
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = stringResource(id = R.string.tickets_title),
                        fontWeight = FontWeight.Bold
                    )
                },
                actions = {
                    IconButton(onClick = { viewModel.loadTickets(forceRefresh = true) }) {
                        Icon(
                            imageVector = Icons.Default.Refresh,
                            contentDescription = stringResource(id = R.string.retry)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primary,
                    titleContentColor = MaterialTheme.colorScheme.onPrimary,
                    actionIconContentColor = MaterialTheme.colorScheme.onPrimary
                )
            )
        }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            Column(modifier = Modifier.fillMaxSize()) {
                // Banner chế độ Offline khi mất mạng
                if (uiState.isOffline) {
                    Surface(
                        color = MaterialTheme.colorScheme.errorContainer,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 16.dp, vertical = 8.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                imageVector = Icons.Default.Info,
                                contentDescription = null,
                                tint = MaterialTheme.colorScheme.onErrorContainer,
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = stringResource(id = R.string.tickets_offline_banner),
                                color = MaterialTheme.colorScheme.onErrorContainer,
                                style = MaterialTheme.typography.bodySmall,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }
                }

                // TabRow phân loại: "Sắp chiếu" (Active/Valid) và "Đã xem" (Used/Expired)
                PrimaryTabRow(
                    selectedTabIndex = if (uiState.selectedTab == TicketFilterTab.UPCOMING) 0 else 1
                ) {
                    Tab(
                        selected = uiState.selectedTab == TicketFilterTab.UPCOMING,
                        onClick = { viewModel.onSelectTab(TicketFilterTab.UPCOMING) },
                        text = {
                            Text(
                                text = "Sắp chiếu (${uiState.upcomingCount})",
                                fontWeight = if (uiState.selectedTab == TicketFilterTab.UPCOMING) FontWeight.Bold else FontWeight.Normal
                            )
                        }
                    )
                    Tab(
                        selected = uiState.selectedTab == TicketFilterTab.HISTORY,
                        onClick = { viewModel.onSelectTab(TicketFilterTab.HISTORY) },
                        text = {
                            Text(
                                text = "Đã xem (${uiState.historyCount})",
                                fontWeight = if (uiState.selectedTab == TicketFilterTab.HISTORY) FontWeight.Bold else FontWeight.Normal
                            )
                        }
                    )
                }

                PullToRefreshBox(
                    isRefreshing = uiState.isLoading,
                    onRefresh = { viewModel.loadTickets(forceRefresh = true) },
                    modifier = Modifier.fillMaxSize()
                ) {
                    val tickets = uiState.filteredTickets

                    when {
                        uiState.isLoading && tickets.isEmpty() -> {
                            Box(
                                modifier = Modifier.fillMaxSize(),
                                contentAlignment = Alignment.Center
                            ) {
                                CircularProgressIndicator()
                            }
                        }
                        tickets.isEmpty() -> {
                            EmptyTicketsView(
                                selectedTab = uiState.selectedTab,
                                onNavigateToHome = onNavigateToHome
                            )
                        }
                        else -> {
                            LazyColumn(
                                modifier = Modifier.fillMaxSize(),
                                contentPadding = PaddingValues(16.dp),
                                verticalArrangement = Arrangement.spacedBy(16.dp)
                            ) {
                                items(tickets, key = { it.invoiceId }) { ticket ->
                                    CinemaTicketCard(
                                        ticket = ticket,
                                        isUpcoming = uiState.selectedTab == TicketFilterTab.UPCOMING,
                                        onViewQrClick = { viewModel.onSelectTicket(ticket) },
                                        onCardClick = { viewModel.onSelectTicket(ticket) }
                                    )
                                }
                            }
                        }
                    }
                }
            }

            // BottomSheet hiển thị chi tiết mã QR khi khách hàng chọn vé
            uiState.selectedTicket?.let { selected ->
                TicketDetailBottomSheet(
                    ticket = selected,
                    ticketDetails = uiState.selectedTicketDetails,
                    selectedSeatIndex = uiState.selectedSeatIndex,
                    onSelectSeatIndex = { viewModel.onSelectSeatIndex(it) },
                    onDismiss = { viewModel.onDismissDetail() }
                )
            }
        }
    }
}

/**
 * Thẻ vé xem phim dạng đục lỗ răng cưa đặc trưng rạp phim (Cinema Ticket Card UI).
 */
@Composable
fun CinemaTicketCard(
    ticket: TicketEntity,
    isUpcoming: Boolean,
    onViewQrClick: () -> Unit,
    onCardClick: () -> Unit
) {
    val showDateTimeText = DateTimeUtils.formatShowDateTime(ticket.showDate, ticket.startTime)
    val totalAmountText = DateTimeUtils.formatCurrency(ticket.totalAmount)
    val cornerRadiusPx = 36f
    val cutoutRadiusPx = 28f
    val cutoutFraction = 0.68f

    val ticketShape = remember(cornerRadiusPx, cutoutRadiusPx, cutoutFraction) {
        TicketCutoutShape(cornerRadiusPx, cutoutRadiusPx, cutoutFraction)
    }

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(4.dp, shape = ticketShape)
            .clip(ticketShape)
            .background(MaterialTheme.colorScheme.surface)
            .clickable(onClick = onCardClick)
    ) {
        Column(modifier = Modifier.fillMaxWidth()) {
            // Phần trên thân vé: Thông tin phim, suất chiếu, phòng chiếu, ghế
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 16.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "MÃ VÉ: ${ticket.invoiceId}",
                        style = MaterialTheme.typography.labelMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary,
                        letterSpacing = 0.5.sp
                    )
                    TicketStatusBadge(status = ticket.status, isUpcoming = isUpcoming)
                }

                Spacer(modifier = Modifier.height(10.dp))

                Text(
                    text = ticket.movieTitle ?: "—",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(modifier = Modifier.height(12.dp))

                // Suất chiếu
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        imageVector = Icons.Default.DateRange,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(16.dp)
                    )
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(
                        text = showDateTimeText,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium
                    )
                }

                Spacer(modifier = Modifier.height(6.dp))

                // Phòng chiếu và Ghế
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            imageVector = Icons.Default.Place,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.size(16.dp)
                        )
                        Spacer(modifier = Modifier.width(6.dp))
                        Text(
                            text = ticket.roomName ?: "Phòng chiếu CinemaBD",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }

                    Surface(
                        color = MaterialTheme.colorScheme.primaryContainer,
                        shape = RoundedCornerShape(8.dp)
                    ) {
                        Text(
                            text = "Ghế: ${ticket.seats.ifBlank { "—" }}",
                            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                            style = MaterialTheme.typography.labelMedium,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                }
            }

            // Đường nét đứt đục lỗ giữa thân vé và cuống vé
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(20.dp),
                contentAlignment = Alignment.Center
            ) {
                Canvas(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 24.dp)
                        .height(1.dp)
                ) {
                    drawLine(
                        color = Color.LightGray,
                        start = Offset(0f, 0f),
                        end = Offset(size.width, 0f),
                        pathEffect = PathEffect.dashPathEffect(floatArrayOf(12f, 10f), 0f),
                        strokeWidth = 2f
                    )
                }
            }

            // Phần dưới cuống vé: Tổng tiền và Nút xem mã QR
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 14.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column {
                    Text(
                        text = "Tổng tiền",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = totalAmountText,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                }

                if (isUpcoming) {
                    Button(
                        onClick = onViewQrClick,
                        colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary),
                        shape = RoundedCornerShape(12.dp),
                        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 8.dp)
                    ) {
                        Icon(
                            painter = painterResource(id = R.drawable.ic_qr_code),
                            contentDescription = null,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(6.dp))
                        Text(
                            text = "Xem mã QR",
                            style = MaterialTheme.typography.labelLarge,
                            fontWeight = FontWeight.Bold
                        )
                    }
                } else {
                    OutlinedButton(
                        onClick = onViewQrClick,
                        shape = RoundedCornerShape(12.dp),
                        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 8.dp)
                    ) {
                        Text(
                            text = "Chi tiết vé",
                            style = MaterialTheme.typography.labelMedium
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Icon(
                            imageVector = Icons.AutoMirrored.Filled.ArrowForward,
                            contentDescription = null,
                            modifier = Modifier.size(14.dp)
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun TicketStatusBadge(
    status: String?,
    isUpcoming: Boolean
) {
    val displayStatus = when {
        status?.contains("check", ignoreCase = true) == true -> "Đã check-in"
        status?.contains("paid", ignoreCase = true) == true ||
                status?.contains("thành công", ignoreCase = true) == true ||
                status?.contains("đã thanh toán", ignoreCase = true) == true -> if (isUpcoming) "Hợp lệ" else "Đã sử dụng"
        status?.contains("hủy", ignoreCase = true) == true -> "Đã hủy"
        else -> status ?: if (isUpcoming) "Hợp lệ" else "Đã kết thúc"
    }

    val (bg, textCol) = when {
        displayStatus == "Hợp lệ" -> Color(0xFFE8F5E9) to Color(0xFF2E7D32) // Xanh lá
        displayStatus == "Đã check-in" -> Color(0xFFE3F2FD) to Color(0xFF1565C0) // Xanh dương
        displayStatus == "Đã sử dụng" || displayStatus == "Đã kết thúc" -> Color(0xFFEEEEEE) to Color(0xFF616161) // Xám
        else -> Color(0xFFFFF3E0) to Color(0xFFE65100) // Cam
    }

    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(10.dp))
            .background(bg)
            .padding(horizontal = 8.dp, vertical = 4.dp)
    ) {
        Text(
            text = displayStatus,
            color = textCol,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold
        )
    }
}

/**
 * Shape vẽ vé xem phim có 2 lỗ bán nguyệt (cutouts) ở 2 mép bên trái và bên phải.
 */
class TicketCutoutShape(
    private val cornerRadius: Float,
    private val cutoutRadius: Float,
    private val cutoutFraction: Float = 0.68f
) : Shape {
    override fun createOutline(
        size: Size,
        layoutDirection: LayoutDirection,
        density: Density
    ): Outline {
        val path = Path().apply {
            val w = size.width
            val h = size.height
            val cutoutY = h * cutoutFraction

            // Bắt đầu từ góc trên bên trái
            moveTo(cornerRadius, 0f)
            lineTo(w - cornerRadius, 0f)
            arcTo(
                rect = Rect(w - cornerRadius * 2, 0f, w, cornerRadius * 2),
                startAngleDegrees = 270f,
                sweepAngleDegrees = 90f,
                forceMoveTo = false
            )
            // Xuống mép phải tới lỗ khoét phải
            lineTo(w, cutoutY - cutoutRadius)
            arcTo(
                rect = Rect(w - cutoutRadius, cutoutY - cutoutRadius, w + cutoutRadius, cutoutY + cutoutRadius),
                startAngleDegrees = 270f,
                sweepAngleDegrees = -180f,
                forceMoveTo = false
            )
            // Xuống góc dưới bên phải
            lineTo(w, h - cornerRadius)
            arcTo(
                rect = Rect(w - cornerRadius * 2, h - cornerRadius * 2, w, h),
                startAngleDegrees = 0f,
                sweepAngleDegrees = 90f,
                forceMoveTo = false
            )
            // Sang góc dưới bên trái
            lineTo(cornerRadius, h)
            arcTo(
                rect = Rect(0f, h - cornerRadius * 2, cornerRadius * 2, h),
                startAngleDegrees = 90f,
                sweepAngleDegrees = 90f,
                forceMoveTo = false
            )
            // Lên mép trái tới lỗ khoét trái
            lineTo(0f, cutoutY + cutoutRadius)
            arcTo(
                rect = Rect(-cutoutRadius, cutoutY - cutoutRadius, cutoutRadius, cutoutY + cutoutRadius),
                startAngleDegrees = 90f,
                sweepAngleDegrees = -180f,
                forceMoveTo = false
            )
            // Lên lại góc trên bên trái
            lineTo(0f, cornerRadius)
            arcTo(
                rect = Rect(0f, 0f, cornerRadius * 2, cornerRadius * 2),
                startAngleDegrees = 180f,
                sweepAngleDegrees = 90f,
                forceMoveTo = false
            )
            close()
        }
        return Outline.Generic(path)
    }
}

/**
 * Giao diện Empty State khi khách hàng chưa mua vé nào.
 */
@Composable
private fun EmptyTicketsView(
    selectedTab: TicketFilterTab,
    onNavigateToHome: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Surface(
            modifier = Modifier.size(80.dp),
            shape = RoundedCornerShape(40.dp),
            color = MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.5f)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Icon(
                    painter = painterResource(id = R.drawable.ic_qr_code),
                    contentDescription = null,
                    modifier = Modifier.size(40.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        Text(
            text = if (selectedTab == TicketFilterTab.UPCOMING) {
                "Bạn chưa có vé xem phim sắp tới"
            } else {
                "Chưa có lịch sử vé đã xem"
            },
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            textAlign = androidx.compose.ui.text.style.TextAlign.Center
        )

        Spacer(modifier = Modifier.height(8.dp))

        Text(
            text = "Hãy khám phá các bộ phim bom tấn đang chiếu và đặt vé ngay để nhận mã QR vào rạp nhanh chóng.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = androidx.compose.ui.text.style.TextAlign.Center
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = onNavigateToHome,
            shape = RoundedCornerShape(12.dp)
        ) {
            Text(
                text = "Khám phá phim ngay",
                fontWeight = FontWeight.Bold
            )
        }
    }
}
