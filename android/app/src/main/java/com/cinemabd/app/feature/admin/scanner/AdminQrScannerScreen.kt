package com.cinemabd.app.feature.admin.scanner

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.BackHandler
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.ClipOp
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipPath
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.cinemabd.app.R
import com.cinemabd.app.feature.admin.AdminLogoutDialog
import com.cinemabd.app.feature.admin.model.CheckInResultData
import java.util.concurrent.Executors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AdminQrScannerScreen(
    navController: NavController? = null,
    viewModel: AdminScanTicketViewModel = hiltViewModel(),
    adminName: String? = null,
    onBack: (() -> Unit)? = null,
    onLogout: () -> Unit
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current

    var hasScannedTicket by remember { mutableStateOf(false) }
    var showLogoutDialog by remember { mutableStateOf(false) }

    LaunchedEffect(uiState.scanStatus) {
        if (uiState.scanStatus is ScanStatus.Valid) {
            hasScannedTicket = true
        }
    }

    val finishWithScannedFlag = { scanned: Boolean ->
        if (scanned) {
            navController?.previousBackStackEntry?.savedStateHandle?.set("ticket_scanned", true)
        }
        val popped = navController?.popBackStack() ?: false
        if (!popped) {
            onBack?.invoke()
        }
    }

    BackHandler {
        finishWithScannedFlag(hasScannedTicket)
    }

    // QR analyzer instance
    val qrCodeAnalyzer = remember {
        QrCodeAnalyzer { qrText ->
            viewModel.onQrScanned(qrText)
        }
    }

    // Pause/resume analyzer based on scan status
    LaunchedEffect(uiState.scanStatus) {
        when (uiState.scanStatus) {
            is ScanStatus.Idle -> qrCodeAnalyzer.resume()
            else -> qrCodeAnalyzer.pause()
        }
    }

    // Camera reference for torch control
    var camera by remember { mutableStateOf<androidx.camera.core.Camera?>(null) }

    // Torch control
    LaunchedEffect(uiState.isTorchEnabled) {
        camera?.cameraControl?.enableTorch(uiState.isTorchEnabled)
    }

    // Permission launcher
    val permissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission()
    ) { isGranted ->
        viewModel.onCameraPermissionResult(isGranted)
    }

    // Check permission on first composition
    LaunchedEffect(Unit) {
        val granted = ContextCompat.checkSelfPermission(
            context, Manifest.permission.CAMERA
        ) == PackageManager.PERMISSION_GRANTED
        viewModel.onCameraPermissionResult(granted)
        if (!granted) {
            permissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

    // Logout dialog
    if (showLogoutDialog) {
        AdminLogoutDialog(
            onConfirm = {
                showLogoutDialog = false
                onLogout()
            },
            onDismiss = { showLogoutDialog = false }
        )
    }

    if (!uiState.isCameraPermissionGranted) {
        // Permission denied fallback UI
        PermissionDeniedContent(
            onRequestPermission = { permissionLauncher.launch(Manifest.permission.CAMERA) },
            onLogout = { showLogoutDialog = true }
        )
    } else {
        // Main scanner UI
        Box(modifier = Modifier.fillMaxSize()) {
            // Layer 1: Camera Preview
            AndroidView(
                factory = { ctx ->
                    PreviewView(ctx).apply {
                        implementationMode = PreviewView.ImplementationMode.COMPATIBLE
                        scaleType = PreviewView.ScaleType.FILL_CENTER
                    }
                },
                modifier = Modifier.fillMaxSize(),
                update = { previewView ->
                    val cameraProviderFuture = ProcessCameraProvider.getInstance(context)
                    cameraProviderFuture.addListener({
                        val cameraProvider = cameraProviderFuture.get()
                        val preview = Preview.Builder().build().also {
                            it.surfaceProvider = previewView.surfaceProvider
                        }
                        val imageAnalysis = ImageAnalysis.Builder()
                            .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                            .build()
                            .also {
                                it.setAnalyzer(
                                    Executors.newSingleThreadExecutor(),
                                    qrCodeAnalyzer
                                )
                            }

                        try {
                            cameraProvider.unbindAll()
                            camera = cameraProvider.bindToLifecycle(
                                lifecycleOwner,
                                CameraSelector.DEFAULT_BACK_CAMERA,
                                preview,
                                imageAnalysis
                            )
                        } catch (_: Exception) {
                            // Camera binding failed
                        }
                    }, ContextCompat.getMainExecutor(context))
                }
            )

            // Layer 2: Scanner Overlay
            ScannerOverlay()

            // Layer 3: Top Toolbar
            ScannerTopBar(
                isTorchEnabled = uiState.isTorchEnabled,
                onToggleTorch = viewModel::toggleTorch,
                onBack = { finishWithScannedFlag(hasScannedTicket) },
                onLogout = { showLogoutDialog = true },
                adminName = adminName
            )

            // Layer 4: Loading overlay
            if (uiState.scanStatus is ScanStatus.Verifying) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(Color.Black.copy(alpha = 0.5f)),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        CircularProgressIndicator(color = Color.White)
                        Spacer(modifier = Modifier.height(16.dp))
                        Text(
                            text = stringResource(R.string.admin_scan_verifying),
                            color = Color.White,
                            style = MaterialTheme.typography.bodyLarge
                        )
                    }
                }
            }

            // Layer 5: Result ModalBottomSheet
            val scanStatus = uiState.scanStatus
            val showSheet = scanStatus is ScanStatus.Valid ||
                    scanStatus is ScanStatus.AlreadyUsed ||
                    scanStatus is ScanStatus.InvalidOrTampered ||
                    scanStatus is ScanStatus.NotFound ||
                    scanStatus is ScanStatus.Error

            if (showSheet) {
                val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
                ModalBottomSheet(
                    onDismissRequest = { viewModel.resetScanner() },
                    sheetState = sheetState
                ) {
                    ScanResultContent(
                        scanStatus = scanStatus,
                        onConfirm = {
                            hasScannedTicket = true
                            viewModel.resetScanner()
                            finishWithScannedFlag(true)
                        },
                        onContinueScan = {
                            hasScannedTicket = true
                            viewModel.resetScanner()
                        },
                        onRetry = { viewModel.resetScanner() }
                    )
                }
            }
        }
    }

    // Cleanup camera on dispose
    DisposableEffect(Unit) {
        onDispose {
            camera?.cameraControl?.enableTorch(false)
        }
    }
}

@Composable
private fun ScannerOverlay() {
    val infiniteTransition = rememberInfiniteTransition(label = "scanLine")
    val scanLineProgress by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 2000, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "scanLineAnim"
    )

    Canvas(modifier = Modifier.fillMaxSize()) {
        val canvasWidth = size.width
        val canvasHeight = size.height

        // Scanner cutout dimensions
        val cutoutSize = minOf(canvasWidth, canvasHeight) * 0.65f
        val cutoutLeft = (canvasWidth - cutoutSize) / 2f
        val cutoutTop = (canvasHeight - cutoutSize) / 2f - 40.dp.toPx()
        val cornerRadius = 16.dp.toPx()

        // Dark overlay with cutout
        val cutoutPath = Path().apply {
            addRoundRect(
                RoundRect(
                    rect = Rect(
                        offset = Offset(cutoutLeft, cutoutTop),
                        size = Size(cutoutSize, cutoutSize)
                    ),
                    cornerRadius = CornerRadius(cornerRadius, cornerRadius)
                )
            )
        }

        clipPath(cutoutPath, clipOp = ClipOp.Difference) {
            drawRect(color = Color.Black.copy(alpha = 0.6f))
        }

        // Corner brackets
        val bracketLength = 32.dp.toPx()
        val bracketStroke = 3.dp.toPx()
        val bracketColor = Color.White

        // Top-left corner
        drawLine(bracketColor, Offset(cutoutLeft, cutoutTop + cornerRadius), Offset(cutoutLeft, cutoutTop + bracketLength), strokeWidth = bracketStroke, cap = StrokeCap.Round)
        drawLine(bracketColor, Offset(cutoutLeft + cornerRadius, cutoutTop), Offset(cutoutLeft + bracketLength, cutoutTop), strokeWidth = bracketStroke, cap = StrokeCap.Round)

        // Top-right corner
        drawLine(bracketColor, Offset(cutoutLeft + cutoutSize, cutoutTop + cornerRadius), Offset(cutoutLeft + cutoutSize, cutoutTop + bracketLength), strokeWidth = bracketStroke, cap = StrokeCap.Round)
        drawLine(bracketColor, Offset(cutoutLeft + cutoutSize - cornerRadius, cutoutTop), Offset(cutoutLeft + cutoutSize - bracketLength, cutoutTop), strokeWidth = bracketStroke, cap = StrokeCap.Round)

        // Bottom-left corner
        drawLine(bracketColor, Offset(cutoutLeft, cutoutTop + cutoutSize - cornerRadius), Offset(cutoutLeft, cutoutTop + cutoutSize - bracketLength), strokeWidth = bracketStroke, cap = StrokeCap.Round)
        drawLine(bracketColor, Offset(cutoutLeft + cornerRadius, cutoutTop + cutoutSize), Offset(cutoutLeft + bracketLength, cutoutTop + cutoutSize), strokeWidth = bracketStroke, cap = StrokeCap.Round)

        // Bottom-right corner
        drawLine(bracketColor, Offset(cutoutLeft + cutoutSize, cutoutTop + cutoutSize - cornerRadius), Offset(cutoutLeft + cutoutSize, cutoutTop + cutoutSize - bracketLength), strokeWidth = bracketStroke, cap = StrokeCap.Round)
        drawLine(bracketColor, Offset(cutoutLeft + cutoutSize - cornerRadius, cutoutTop + cutoutSize), Offset(cutoutLeft + cutoutSize - bracketLength, cutoutTop + cutoutSize), strokeWidth = bracketStroke, cap = StrokeCap.Round)

        // Animated scan line
        val scanLineY = cutoutTop + (cutoutSize * scanLineProgress)
        val scanLinePadding = 8.dp.toPx()
        drawLine(
            color = Color(0xFF4CAF50).copy(alpha = 0.8f),
            start = Offset(cutoutLeft + scanLinePadding, scanLineY),
            end = Offset(cutoutLeft + cutoutSize - scanLinePadding, scanLineY),
            strokeWidth = 2.dp.toPx(),
            cap = StrokeCap.Round
        )

        // Cutout border
        drawRoundRect(
            color = Color.White.copy(alpha = 0.3f),
            topLeft = Offset(cutoutLeft, cutoutTop),
            size = Size(cutoutSize, cutoutSize),
            cornerRadius = CornerRadius(cornerRadius, cornerRadius),
            style = Stroke(width = 1.dp.toPx())
        )
    }
}

@Composable
private fun ScannerTopBar(
    isTorchEnabled: Boolean,
    onToggleTorch: () -> Unit,
    onBack: (() -> Unit)? = null,
    onLogout: () -> Unit,
    adminName: String? = null
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color.Black.copy(alpha = 0.4f))
            .padding(horizontal = 4.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        if (onBack != null) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                    contentDescription = stringResource(R.string.back_btn),
                    tint = Color.White
                )
            }
        } else {
            Spacer(modifier = Modifier.width(8.dp))
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = stringResource(R.string.admin_scan_title),
                color = Color.White,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )
            if (!adminName.isNullOrBlank()) {
                Text(
                    text = adminName,
                    color = Color.White.copy(alpha = 0.8f),
                    style = MaterialTheme.typography.bodySmall
                )
            }
        }
        IconButton(onClick = onToggleTorch) {
            Icon(
                painter = painterResource(
                    id = if (isTorchEnabled) R.drawable.ic_flash_off else R.drawable.ic_flash_on
                ),
                contentDescription = if (isTorchEnabled) {
                    stringResource(R.string.admin_scan_flash_off)
                } else {
                    stringResource(R.string.admin_scan_flash_on)
                },
                tint = if (isTorchEnabled) Color.Yellow else Color.White
            )
        }
        IconButton(onClick = onLogout) {
            Icon(
                imageVector = Icons.AutoMirrored.Filled.Logout,
                contentDescription = stringResource(R.string.admin_logout_cd),
                tint = Color.White
            )
        }
    }
}

@Composable
private fun PermissionDeniedContent(
    onRequestPermission: () -> Unit,
    onLogout: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(
            imageVector = Icons.Default.Warning,
            contentDescription = null,
            modifier = Modifier.size(64.dp),
            tint = MaterialTheme.colorScheme.error
        )
        Spacer(modifier = Modifier.height(24.dp))
        Text(
            text = stringResource(R.string.admin_scan_permission_title),
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center
        )
        Spacer(modifier = Modifier.height(12.dp))
        Text(
            text = stringResource(R.string.admin_scan_permission_message),
            style = MaterialTheme.typography.bodyLarge,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(24.dp))
        Button(onClick = onRequestPermission) {
            Text(stringResource(R.string.admin_scan_permission_btn))
        }
        Spacer(modifier = Modifier.height(16.dp))
        androidx.compose.material3.OutlinedButton(onClick = onLogout) {
            Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = null)
            Spacer(modifier = Modifier.width(8.dp))
            Text(stringResource(R.string.profile_logout_btn))
        }
    }
}

@Composable
private fun ScanResultContent(
    scanStatus: ScanStatus,
    onConfirm: () -> Unit,
    onContinueScan: () -> Unit = {},
    onRetry: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 16.dp)
    ) {
        when (scanStatus) {
            is ScanStatus.Valid -> {
                ValidTicketResult(data = scanStatus.data, message = scanStatus.message)
                Spacer(modifier = Modifier.height(20.dp))
                Button(
                    onClick = onConfirm,
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0xFF2E7D32)
                    ),
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Icon(Icons.Default.Check, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        text = stringResource(R.string.admin_scan_confirm_btn),
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = onContinueScan,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Text(
                        text = stringResource(R.string.admin_scan_continue_btn),
                        fontSize = 15.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            is ScanStatus.AlreadyUsed -> {
                AlreadyUsedResult(data = scanStatus.data, message = scanStatus.message)
                Spacer(modifier = Modifier.height(20.dp))
                RetryButton(onClick = onRetry)
            }

            is ScanStatus.InvalidOrTampered -> {
                ErrorResult(
                    title = stringResource(R.string.admin_scan_invalid_title),
                    message = scanStatus.message,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(20.dp))
                RetryButton(onClick = onRetry)
            }

            is ScanStatus.NotFound -> {
                ErrorResult(
                    title = stringResource(R.string.check_in_not_found_title),
                    message = scanStatus.message,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(20.dp))
                RetryButton(onClick = onRetry)
            }

            is ScanStatus.Error -> {
                ErrorResult(
                    title = stringResource(R.string.admin_scan_error_title),
                    message = scanStatus.message,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(20.dp))
                RetryButton(onClick = onRetry)
            }

            else -> {}
        }

        Spacer(modifier = Modifier.height(16.dp))
    }
}

@Composable
private fun ValidTicketResult(data: CheckInResultData, message: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = Icons.Default.Check,
            contentDescription = null,
            tint = Color(0xFF2E7D32),
            modifier = Modifier.size(32.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Text(
            text = stringResource(R.string.admin_scan_valid_title),
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            color = Color(0xFF2E7D32)
        )
    }

    Spacer(modifier = Modifier.height(8.dp))
    Text(
        text = message,
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
    )

    Spacer(modifier = Modifier.height(16.dp))
    HorizontalDivider()
    Spacer(modifier = Modifier.height(12.dp))

    TicketDetailRow(label = stringResource(R.string.check_in_ticket_id_label), value = data.ticketId.orEmpty())
    TicketDetailRow(label = stringResource(R.string.check_in_movie_label), value = data.movieTitle.orEmpty())
    if (!data.showDate.isNullOrBlank() || !data.startTime.isNullOrBlank()) {
        TicketDetailRow(
            label = stringResource(R.string.admin_scan_showtime_label),
            value = buildString {
                data.showDate?.let { append(it) }
                data.startTime?.let {
                    if (isNotEmpty()) append(" • ")
                    append(it)
                }
            }
        )
    }
    TicketDetailRow(label = stringResource(R.string.check_in_seat_label), value = data.seatId.orEmpty())
    if (!data.customerName.isNullOrBlank()) {
        TicketDetailRow(label = stringResource(R.string.check_in_customer_label), value = data.customerName)
    }
}

@Composable
private fun AlreadyUsedResult(data: CheckInResultData?, message: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = Icons.Default.Info,
            contentDescription = null,
            tint = Color(0xFFEF6C00),
            modifier = Modifier.size(32.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Text(
            text = stringResource(R.string.admin_scan_already_used_title),
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            color = Color(0xFFEF6C00)
        )
    }

    Spacer(modifier = Modifier.height(8.dp))
    Text(
        text = message,
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
    )

    if (data != null) {
        Spacer(modifier = Modifier.height(16.dp))
        HorizontalDivider()
        Spacer(modifier = Modifier.height(12.dp))

        if (!data.ticketId.isNullOrBlank()) {
            TicketDetailRow(label = stringResource(R.string.check_in_ticket_id_label), value = data.ticketId)
        }
        if (!data.movieTitle.isNullOrBlank()) {
            TicketDetailRow(label = stringResource(R.string.check_in_movie_label), value = data.movieTitle)
        }
        if (!data.seatId.isNullOrBlank()) {
            TicketDetailRow(label = stringResource(R.string.check_in_seat_label), value = data.seatId)
        }
        if (!data.checkedInAt.isNullOrBlank()) {
            TicketDetailRow(label = stringResource(R.string.admin_scan_checked_in_at_label), value = data.checkedInAt)
        }
    }
}

@Composable
private fun ErrorResult(title: String, message: String, color: Color) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = Icons.Default.Warning,
            contentDescription = null,
            tint = color,
            modifier = Modifier.size(32.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Text(
            text = title,
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            color = color
        )
    }

    Spacer(modifier = Modifier.height(8.dp))
    Text(
        text = message,
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
    )
}

@Composable
private fun TicketDetailRow(label: String, value: String) {
    if (value.isNotBlank()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 4.dp),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = value,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSurface
            )
        }
    }
}

@Composable
private fun RetryButton(onClick: () -> Unit) {
    Button(
        onClick = onClick,
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp)
    ) {
        Text(
            text = stringResource(R.string.admin_scan_retry_btn),
            fontSize = 16.sp,
            fontWeight = FontWeight.Bold
        )
    }
}
