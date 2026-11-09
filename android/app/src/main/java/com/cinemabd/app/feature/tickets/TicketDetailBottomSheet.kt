package com.cinemabd.app.feature.tickets

import android.graphics.Bitmap
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Info
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.cinemabd.app.R
import com.cinemabd.app.core.database.entity.TicketDetailEntity
import com.cinemabd.app.core.database.entity.TicketEntity
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.core.utils.QrCodeGenerator
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TicketDetailBottomSheet(
    ticket: TicketEntity,
    ticketDetails: List<TicketDetailEntity>,
    selectedSeatIndex: Int,
    onSelectSeatIndex: (Int) -> Unit,
    onDismiss: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    // Xác định nội dung QR cần vẽ
    val currentDetail = ticketDetails.getOrNull(selectedSeatIndex)
    val qrContent = currentDetail?.qrPayload?.takeIf { it.isNotBlank() }
        ?: ticket.qrPayload?.takeIf { it.isNotBlank() }
        ?: "CinemaBD|CHECKIN|${ticket.invoiceId}|${currentDetail?.ticketId ?: ticket.invoiceId}"

    var qrBitmap by remember(qrContent) { mutableStateOf<Bitmap?>(null) }
    var isGeneratingQr by remember(qrContent) { mutableStateOf(true) }

    LaunchedEffect(qrContent) {
        isGeneratingQr = true
        qrBitmap = withContext(Dispatchers.Default) {
            QrCodeGenerator.generateQrCodeBitmap(qrContent, sizePx = 600)
        }
        isGeneratingQr = false
    }

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState,
        dragHandle = { BottomSheetDefaults.DragHandle() },
        containerColor = MaterialTheme.colorScheme.surface
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header: Tiêu đề và nút đóng
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = stringResource(id = R.string.ticket_qr_title),
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = ticket.movieTitle ?: stringResource(id = R.string.app_name),
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.primary,
                        fontWeight = FontWeight.SemiBold
                    )
                }
                IconButton(onClick = onDismiss) {
                    Icon(
                        imageVector = Icons.Default.Close,
                        contentDescription = stringResource(id = R.string.cancel)
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            // Bộ chọn ghế nếu đơn hàng có nhiều vé
            if (ticketDetails.size > 1) {
                Text(
                    text = "Chọn vé để hiển thị mã QR tương ứng:",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.fillMaxWidth()
                )
                Spacer(modifier = Modifier.height(6.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    ticketDetails.forEachIndexed { index, detail ->
                        FilterChip(
                            selected = selectedSeatIndex == index,
                            onClick = { onSelectSeatIndex(index) },
                            label = {
                                Text(
                                    text = "Ghế ${detail.seatId}",
                                    fontWeight = if (selectedSeatIndex == index) FontWeight.Bold else FontWeight.Normal
                                )
                            }
                        )
                    }
                }
                Spacer(modifier = Modifier.height(16.dp))
            }

            // Khung nền trắng tương phản cao hiển thị mã QR cỡ lớn
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .wrapContentHeight(),
                shape = RoundedCornerShape(20.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 6.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Box(
                        modifier = Modifier
                            .size(240.dp)
                            .background(Color.White)
                            .padding(8.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        when {
                            isGeneratingQr -> {
                                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
                            }
                            qrBitmap != null -> {
                                Image(
                                    bitmap = qrBitmap!!.asImageBitmap(),
                                    contentDescription = stringResource(id = R.string.ticket_qr_title),
                                    modifier = Modifier.fillMaxSize()
                                )
                            }
                            else -> {
                                Text(
                                    text = stringResource(id = R.string.ticket_qr_error),
                                    color = MaterialTheme.colorScheme.error,
                                    style = MaterialTheme.typography.bodyMedium
                                )
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Mã tra cứu bằng chữ số (Booking Code / Ticket ID) để dự phòng
                    val lookupCode = currentDetail?.ticketId?.ifBlank { ticket.invoiceId } ?: ticket.invoiceId
                    Surface(
                        color = Color(0xFFF5F5F5),
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(vertical = 10.dp, horizontal = 12.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "MÃ TRA CỨU DỰ PHÒNG",
                                style = MaterialTheme.typography.labelSmall,
                                color = Color.Gray,
                                fontWeight = FontWeight.SemiBold,
                                letterSpacing = 1.sp
                            )
                            Spacer(modifier = Modifier.height(4.dp))
                            Text(
                                text = lookupCode,
                                style = MaterialTheme.typography.titleMedium,
                                fontFamily = FontFamily.Monospace,
                                fontWeight = FontWeight.Bold,
                                color = Color.Black,
                                letterSpacing = 2.sp
                            )
                            if (currentDetail != null && currentDetail.seatId.isNotBlank()) {
                                Text(
                                    text = "Ghế: ${currentDetail.seatId}",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = Color.DarkGray
                                )
                            }
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Tóm tắt thông tin vé
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
                )
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    InfoRow(
                        label = stringResource(id = R.string.ticket_showtime_label),
                        value = DateTimeUtils.formatShowDateTime(ticket.showDate, ticket.startTime)
                    )
                    InfoRow(
                        label = stringResource(id = R.string.ticket_room_label),
                        value = ticket.roomName ?: "—"
                    )
                    InfoRow(
                        label = stringResource(id = R.string.ticket_seats_label),
                        value = ticket.seats.ifBlank { "—" }
                    )
                    InfoRow(
                        label = stringResource(id = R.string.ticket_total_label),
                        value = DateTimeUtils.formatCurrency(ticket.totalAmount),
                        isHighlight = true
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Thông báo lưu ý cho khách hàng
            Surface(
                color = MaterialTheme.colorScheme.secondaryContainer.copy(alpha = 0.7f),
                shape = RoundedCornerShape(12.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.padding(14.dp),
                    verticalAlignment = Alignment.Top
                ) {
                    Icon(
                        imageVector = Icons.Default.Info,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.onSecondaryContainer,
                        modifier = Modifier
                            .size(20.dp)
                            .padding(top = 2.dp)
                    )
                    Spacer(modifier = Modifier.width(10.dp))
                    Text(
                        text = "Vui lòng xuất trình mã QR này tại quầy soát vé trước giờ chiếu 15 phút. Không chia sẻ mã này cho người khác.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSecondaryContainer,
                        lineHeight = 18.sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(20.dp))

            // Nút đóng
            Button(
                onClick = onDismiss,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp)
            ) {
                Text(
                    text = "Đóng",
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold
                )
            }
        }
    }
}

@Composable
private fun InfoRow(
    label: String,
    value: String,
    isHighlight: Boolean = false
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = value,
            style = if (isHighlight) MaterialTheme.typography.titleMedium else MaterialTheme.typography.bodyMedium,
            fontWeight = if (isHighlight) FontWeight.Bold else FontWeight.Medium,
            color = if (isHighlight) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurface
        )
    }
}
