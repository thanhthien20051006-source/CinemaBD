package com.cinemabd.app.feature.admin

import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics

sealed interface StatisticsUiState {
    data object Loading : StatisticsUiState
    data class Error(val message: String) : StatisticsUiState
    data object Empty : StatisticsUiState
    data class Success(val statistics: AdminRevenueStatistics) : StatisticsUiState
}
