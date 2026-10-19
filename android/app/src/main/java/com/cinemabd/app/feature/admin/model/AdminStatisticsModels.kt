package com.cinemabd.app.feature.admin.model

import kotlinx.serialization.Serializable

@Serializable
data class AdminRevenueStatistics(
    val totalRevenue: Double = 0.0,
    val totalOrders: Int = 0,
    val totalTickets: Int = 0,
    val checkedInTickets: Int = 0,
    val averageOrderValue: Double = 0.0,
    val todayRevenue: Double = 0.0,
    val currentMonthRevenue: Double = 0.0,
    val currentYearRevenue: Double = 0.0,
    val statisticsDate: String,
    val monthlyData: List<AdminRevenueDataPoint> = emptyList(),
    val paymentMethods: List<AdminPaymentMethodStat> = emptyList(),
    val topMovies: List<AdminRevenueDataPoint> = emptyList(),
    val dailyData: List<AdminRevenueDataPoint> = emptyList(),
    val hourlyData: List<AdminRevenueDataPoint> = emptyList(),
    val weekdayData: List<AdminRevenueDataPoint> = emptyList(),
    val topCombos: List<AdminRevenueDataPoint> = emptyList(),
    val topCustomers: List<AdminRevenueDataPoint> = emptyList()
)

@Serializable
data class AdminRevenueDataPoint(
    val label: String,
    val value: Double
)

@Serializable
data class AdminPaymentMethodStat(
    val method: String,
    val orderCount: Int,
    val revenue: Double
)
