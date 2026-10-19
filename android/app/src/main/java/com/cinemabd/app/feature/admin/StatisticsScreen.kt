package com.cinemabd.app.feature.admin

import androidx.compose.foundation.Canvas
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.cinemabd.app.R
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.feature.admin.model.AdminRevenueDataPoint
import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics

@Composable
fun StatisticsScreen(viewModel: StatisticsViewModel = hiltViewModel()) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    Scaffold(topBar = { AdminTopBar(title = stringResource(R.string.admin_statistics_title)) }) { padding ->
        when (val state = uiState) {
            StatisticsUiState.Loading -> Column(Modifier.fillMaxSize().padding(padding)) { LoadingContent() }
            is StatisticsUiState.Error -> Column(Modifier.fillMaxSize().padding(padding)) {
                ErrorContent(state.message, viewModel::loadStatistics)
            }
            StatisticsUiState.Empty -> Column(Modifier.fillMaxSize().padding(padding)) {
                EmptyContent(stringResource(R.string.admin_statistics_empty), viewModel::loadStatistics)
            }
            is StatisticsUiState.Success -> StatisticsContent(state.statistics, Modifier.padding(padding))
        }
    }
}

@Composable
private fun StatisticsContent(statistics: AdminRevenueStatistics, modifier: Modifier = Modifier) {
    LazyColumn(
        modifier = modifier.fillMaxSize().padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Text(
                stringResource(R.string.admin_statistics_year_label, statistics.statisticsDate.take(4)),
                style = MaterialTheme.typography.titleMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 12.dp)
            )
        }
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                KpiCard(stringResource(R.string.admin_stat_revenue), DateTimeUtils.formatCurrency(statistics.totalRevenue))
                Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    KpiCard(
                        stringResource(R.string.admin_stat_tickets),
                        statistics.totalTickets.toString(),
                        Modifier.weight(1f)
                    )
                    KpiCard(
                        stringResource(R.string.admin_stat_invoices),
                        statistics.totalOrders.toString(),
                        Modifier.weight(1f)
                    )
                }
            }
        }
        item {
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.35f))
            ) {
                Column(Modifier.fillMaxWidth().padding(16.dp)) {
                    Text(
                        stringResource(R.string.admin_daily_revenue_title),
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(16.dp))
                    if (statistics.dailyData.isEmpty()) {
                        Text(stringResource(R.string.admin_daily_revenue_empty), color = MaterialTheme.colorScheme.onSurfaceVariant)
                    } else {
                        DailyRevenueChart(
                            points = statistics.dailyData,
                            modifier = Modifier.fillMaxWidth().height(210.dp)
                        )
                        Spacer(Modifier.height(12.dp))
                        DailyRevenueAccessibleTable(statistics.dailyData)
                    }
                }
            }
        }
    }
}

@Composable
private fun DailyRevenueAccessibleTable(points: List<AdminRevenueDataPoint>) {
    val pointRows = points.map { point ->
        stringResource(R.string.admin_daily_revenue_accessible_point, point.label, DateTimeUtils.formatCurrency(point.value))
    }
    Text(
        pointRows.joinToString("\n"),
        style = MaterialTheme.typography.bodySmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant
    )
}

@Composable
private fun KpiCard(label: String, value: String, modifier: Modifier = Modifier) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.48f))
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(8.dp))
            Text(value, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
private fun DailyRevenueChart(points: List<AdminRevenueDataPoint>, modifier: Modifier = Modifier) {
    val primary = MaterialTheme.colorScheme.primary
    val surface = MaterialTheme.colorScheme.surface
    val grid = MaterialTheme.colorScheme.outlineVariant
    val chartTitle = stringResource(R.string.admin_daily_revenue_title)
    val labelDescription = points.joinToString(separator = "; ") { "${it.label}: ${DateTimeUtils.formatCurrency(it.value)}" }
    Box(
        modifier = modifier.semantics {
            contentDescription = "$chartTitle: $labelDescription"
        }
    ) {
        Canvas(modifier = Modifier.fillMaxSize()) {
            val left = 36.dp.toPx()
            val right = 8.dp.toPx()
            val top = 8.dp.toPx()
            val bottom = 28.dp.toPx()
            val plotWidth = (size.width - left - right).coerceAtLeast(1f)
            val plotHeight = (size.height - top - bottom).coerceAtLeast(1f)
            val maxValue = points.maxOfOrNull { it.value }?.coerceAtLeast(1.0)?.toFloat() ?: 1f
            val minValue = points.minOfOrNull { it.value }?.coerceAtMost(0.0)?.toFloat() ?: 0f
            val valueRange = (maxValue - minValue).takeIf { it > 0f } ?: maxValue

            repeat(4) { index ->
                val y = top + plotHeight * index / 3f
                drawLine(grid.copy(alpha = 0.45f), Offset(left, y), Offset(left + plotWidth, y), strokeWidth = 1.dp.toPx())
            }
            val path = Path()
            val positions = points.mapIndexed { index, point ->
                val x = if (points.size == 1) left + plotWidth / 2f else left + plotWidth * index / (points.size - 1f)
                val y = top + plotHeight - ((point.value.toFloat() - minValue) / valueRange * plotHeight)
                Offset(x, y)
            }
            positions.forEachIndexed { index, point ->
                if (index == 0) path.moveTo(point.x, point.y) else path.lineTo(point.x, point.y)
            }
            if (positions.size > 1) {
                drawPath(path, color = primary, style = Stroke(width = 2.dp.toPx()))
            }
            positions.forEach { point ->
                drawCircle(color = surface, radius = 6.dp.toPx(), center = point)
                drawCircle(color = primary, radius = 4.dp.toPx(), center = point)
            }
        }
        if (points.isNotEmpty()) {
            Row(
                modifier = Modifier.align(Alignment.BottomCenter).fillMaxWidth().padding(start = 36.dp, end = 8.dp),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(points.first().label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                if (points.size > 1) {
                    Text(points.last().label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
        }
    }
}
