package com.cinemabd.app.feature.booking

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import java.text.NumberFormat
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CheckoutScreen(
    onBack: () -> Unit,
    onPaymentSuccess: (String) -> Unit,
    showtimeId: String? = null,
    viewModel: CheckoutViewModel = hiltViewModel(),
    bookingSharedViewModel: BookingSharedViewModel
) {
    val cartState by bookingSharedViewModel.cartState.collectAsStateWithLifecycle()
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val context = LocalContext.current

    LaunchedEffect(cartState) {
        viewModel.initFromCart(cartState)
    }

    LaunchedEffect(uiState) {
        if (uiState is CheckoutUiState.Success) {
            val txnRef = (uiState as CheckoutUiState.Success).transactionRef
            onPaymentSuccess(txnRef)
        }
    }

    val currencyFormatter = remember { NumberFormat.getNumberInstance(Locale("vi", "VN")) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(id = R.string.checkout_title)) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = stringResource(id = R.string.back_btn)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                )
            )
        },
        bottomBar = {
            if (uiState is CheckoutUiState.Ready) {
                val ready = uiState as CheckoutUiState.Ready
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    shadowElevation = 8.dp,
                    color = MaterialTheme.colorScheme.surface
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp)
                    ) {
                        if (ready.errorMessage != null) {
                            Text(
                                text = ready.errorMessage,
                                color = MaterialTheme.colorScheme.error,
                                style = MaterialTheme.typography.bodySmall,
                                modifier = Modifier.padding(bottom = 8.dp)
                            )
                        }

                        Button(
                            onClick = {
                                viewModel.submitPayment(context = context)
                            },
                            enabled = !ready.isProcessing && ready.seats.isNotEmpty(),
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(52.dp),
                            shape = RoundedCornerShape(26.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = MaterialTheme.colorScheme.error
                            )
                        ) {
                            if (ready.isProcessing) {
                                CircularProgressIndicator(
                                    modifier = Modifier.size(24.dp),
                                    color = MaterialTheme.colorScheme.onError,
                                    strokeWidth = 2.5.dp
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                                Text(
                                    text = stringResource(id = R.string.pay_processing),
                                    fontWeight = FontWeight.Bold
                                )
                            } else {
                                Text(
                                    text = "${stringResource(id = R.string.pay_btn)} (${currencyFormatter.format(ready.finalTotal.toLong())} đ)",
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 16.sp
                                )
                            }
                        }
                    }
                }
            }
        }
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when (val state = uiState) {
                is CheckoutUiState.Loading -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }

                is CheckoutUiState.Error -> {
                    Column(
                        modifier = Modifier
                            .align(Alignment.Center)
                            .padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = state.message,
                            style = MaterialTheme.typography.bodyLarge,
                            textAlign = TextAlign.Center
                        )
                        Spacer(modifier = Modifier.height(16.dp))
                        Button(onClick = { viewModel.initFromCart(cartState) }) {
                            Text(stringResource(id = R.string.retry))
                        }
                    }
                }

                is CheckoutUiState.Success -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }

                is CheckoutUiState.Ready -> {
                    CheckoutContent(
                        state = state,
                        currencyFormatter = currencyFormatter,
                        onApplyVoucher = { code -> viewModel.applyVoucher(code) },
                        onSelectPaymentMethod = { method -> viewModel.selectPaymentMethod(method) }
                    )
                }
            }
        }
    }
}

@Composable
private fun CheckoutContent(
    state: CheckoutUiState.Ready,
    currencyFormatter: NumberFormat,
    onApplyVoucher: (String) -> Unit,
    onSelectPaymentMethod: (PaymentMethod) -> Unit
) {
    var voucherInput by rememberSaveable(state.voucherCode) { mutableStateOf(state.voucherCode) }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Hold seat timer banner
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.tertiaryContainer
                ),
                shape = RoundedCornerShape(12.dp)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Info,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.onTertiaryContainer,
                        modifier = Modifier.size(24.dp)
                    )
                    Spacer(modifier = Modifier.width(12.dp))
                    Text(
                        text = stringResource(id = R.string.checkout_hold_notice),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onTertiaryContainer
                    )
                }
            }
        }

        // Order Summary Card
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                shape = RoundedCornerShape(12.dp),
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            Icons.Default.ShoppingCart,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(20.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = stringResource(id = R.string.order_summary_title),
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    if (state.movieTitle.isNotBlank()) {
                        SummaryRow(
                            label = stringResource(id = R.string.order_movie_label),
                            value = state.movieTitle,
                            isBold = true
                        )
                    }

                    if (state.showDate.isNotBlank() || state.startTime.isNotBlank()) {
                        val showtimeText = listOf(state.showDate, state.startTime)
                            .filter { it.isNotBlank() }
                            .joinToString(" • ")
                        SummaryRow(
                            label = stringResource(id = R.string.order_showtime_label),
                            value = showtimeText
                        )
                    }

                    if (state.roomName.isNotBlank()) {
                        SummaryRow(
                            label = stringResource(id = R.string.order_room_label),
                            value = state.roomName
                        )
                    } else if (state.showtimeId.isNotBlank()) {
                        SummaryRow(
                            label = stringResource(id = R.string.order_showtime_label),
                            value = state.showtimeId
                        )
                    }

                    Spacer(modifier = Modifier.height(8.dp))

                    // Selected Seats
                    Text(
                        text = stringResource(id = R.string.order_seats_label),
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Spacer(modifier = Modifier.height(6.dp))
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        state.seats.forEach { seat ->
                            Surface(
                                shape = RoundedCornerShape(6.dp),
                                color = MaterialTheme.colorScheme.primaryContainer,
                                modifier = Modifier.padding(vertical = 2.dp)
                            ) {
                                Text(
                                    text = seat,
                                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                                    style = MaterialTheme.typography.labelMedium,
                                    fontWeight = FontWeight.Bold,
                                    color = MaterialTheme.colorScheme.onPrimaryContainer
                                )
                            }
                        }
                    }

                    // Combos
                    Spacer(modifier = Modifier.height(10.dp))
                    Text(
                        text = stringResource(id = R.string.order_combos_label),
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    val activeCombos = state.selectedCombos.filter { it.value > 0 }
                    if (activeCombos.isEmpty()) {
                        Text(
                            text = stringResource(id = R.string.no_combos_selected),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    } else {
                        activeCombos.forEach { (comboId, qty) ->
                            val comboName = state.comboNames[comboId] ?: comboId
                            val comboPrice = (state.comboPrices[comboId] ?: 0.0) * qty
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(vertical = 2.dp),
                                horizontalArrangement = Arrangement.SpaceBetween
                            ) {
                                Text(
                                    text = "$comboName x $qty",
                                    style = MaterialTheme.typography.bodySmall
                                )
                                Text(
                                    text = "${currencyFormatter.format(comboPrice.toLong())} đ",
                                    style = MaterialTheme.typography.bodySmall,
                                    fontWeight = FontWeight.Medium
                                )
                            }
                        }
                    }
                }
            }
        }

        // Voucher Input Card
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                shape = RoundedCornerShape(12.dp),
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            Icons.Default.Star,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(20.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = stringResource(id = R.string.voucher_input_label),
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        OutlinedTextField(
                            value = voucherInput,
                            onValueChange = { voucherInput = it },
                            placeholder = {
                                Text(
                                    stringResource(id = R.string.voucher_input_placeholder),
                                    fontSize = 13.sp
                                )
                            },
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(8.dp)
                        )

                        Button(
                            onClick = { onApplyVoucher(voucherInput) },
                            shape = RoundedCornerShape(8.dp),
                            modifier = Modifier.height(52.dp)
                        ) {
                            Text(stringResource(id = R.string.apply_voucher_btn))
                        }
                    }

                    if (state.discountAmount > 0) {
                        Spacer(modifier = Modifier.height(6.dp))
                        Text(
                            text = stringResource(
                                id = R.string.voucher_applied_msg,
                                state.voucherCode,
                                currencyFormatter.format(state.discountAmount.toLong())
                            ),
                            style = MaterialTheme.typography.bodySmall,
                            color = Color(0xFF16A34A),
                            fontWeight = FontWeight.Medium
                        )
                    }

                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = stringResource(id = R.string.voucher_note),
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }

        // Price Breakdown Card
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                shape = RoundedCornerShape(12.dp),
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(
                        text = stringResource(id = R.string.price_details_title),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )

                    Spacer(modifier = Modifier.height(12.dp))

                    PriceRow(
                        label = stringResource(id = R.string.price_tickets_label),
                        amount = "${currencyFormatter.format(state.ticketTotal.toLong())} đ"
                    )

                    PriceRow(
                        label = stringResource(id = R.string.price_combos_label),
                        amount = "${currencyFormatter.format(state.comboTotal.toLong())} đ"
                    )

                    if (state.discountAmount > 0) {
                        PriceRow(
                            label = stringResource(id = R.string.voucher_discount_label),
                            amount = "-${currencyFormatter.format(state.discountAmount.toLong())} đ",
                            color = Color(0xFF16A34A)
                        )
                    }

                    HorizontalDivider(modifier = Modifier.padding(vertical = 10.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = stringResource(id = R.string.total_payment_label),
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = "${currencyFormatter.format(state.finalTotal.toLong())} đ",
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.error
                        )
                    }
                }
            }
        }

        // Payment Method Selection Card
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                shape = RoundedCornerShape(12.dp),
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(20.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = stringResource(id = R.string.payment_method_title),
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    // Demo Payment Option
                    PaymentMethodOption(
                        title = stringResource(id = R.string.payment_method_demo),
                        description = stringResource(id = R.string.payment_method_demo_desc),
                        selected = state.selectedPaymentMethod == PaymentMethod.DEMO,
                        onSelect = { onSelectPaymentMethod(PaymentMethod.DEMO) }
                    )

                    Spacer(modifier = Modifier.height(10.dp))

                    // VNPay Option
                    PaymentMethodOption(
                        title = stringResource(id = R.string.payment_method_vnpay),
                        description = stringResource(id = R.string.payment_method_vnpay_desc),
                        selected = state.selectedPaymentMethod == PaymentMethod.VNPAY,
                        onSelect = { onSelectPaymentMethod(PaymentMethod.VNPAY) }
                    )
                }
            }
        }

        item { Spacer(modifier = Modifier.height(16.dp)) }
    }
}

@Composable
private fun SummaryRow(label: String, value: String, isBold: Boolean = false) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 3.dp),
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
            fontWeight = if (isBold) FontWeight.Bold else FontWeight.Normal
        )
    }
}

@Composable
private fun PriceRow(label: String, amount: String, color: Color = Color.Unspecified) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 3.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = amount,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold,
            color = color
        )
    }
}

@Composable
private fun PaymentMethodOption(
    title: String,
    description: String,
    selected: Boolean,
    onSelect: () -> Unit
) {
    Surface(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onSelect),
        shape = RoundedCornerShape(10.dp),
        border = BorderStroke(
            width = if (selected) 2.dp else 1.dp,
            color = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.outlineVariant
        ),
        color = if (selected) MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.25f) else MaterialTheme.colorScheme.surface
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            RadioButton(
                selected = selected,
                onClick = onSelect
            )
            Spacer(modifier = Modifier.width(8.dp))
            Column {
                Text(
                    text = title,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Bold,
                    color = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurface
                )
                Text(
                    text = description,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}
