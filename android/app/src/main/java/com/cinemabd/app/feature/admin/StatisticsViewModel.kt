package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics
import dagger.hilt.android.lifecycle.HiltViewModel
import java.time.LocalDate
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class StatisticsViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow<StatisticsUiState>(StatisticsUiState.Loading)
    val uiState: StateFlow<StatisticsUiState> = _uiState.asStateFlow()

    init {
        loadStatistics()
    }

    fun loadStatistics() {
        viewModelScope.launch {
            _uiState.value = StatisticsUiState.Loading
            adminRepository.getRevenueStatistics(LocalDate.now().year).onSuccess { response ->
                val statistics = response.data
                if (!response.success || statistics == null) {
                    _uiState.value = StatisticsUiState.Error(
                        response.message.ifBlank { "Không thể tải thống kê doanh thu." }
                    )
                } else if (isEmptyStatistics(statistics)) {
                    _uiState.value = StatisticsUiState.Empty
                } else {
                    _uiState.value = StatisticsUiState.Success(statistics)
                }
            }.onFailure { error ->
                _uiState.value = StatisticsUiState.Error(
                    error.toUserFriendlyMessage("Không thể tải thống kê doanh thu.")
                )
            }
        }
    }

    private fun isEmptyStatistics(statistics: AdminRevenueStatistics): Boolean =
        statistics.totalRevenue == 0.0 &&
                statistics.totalOrders == 0 &&
                statistics.totalTickets == 0 &&
                statistics.checkedInTickets == 0 &&
                statistics.averageOrderValue == 0.0 &&
                statistics.todayRevenue == 0.0 &&
                statistics.currentMonthRevenue == 0.0 &&
                statistics.currentYearRevenue == 0.0 &&
                statistics.monthlyData.isEmpty() &&
                statistics.paymentMethods.isEmpty() &&
                statistics.topMovies.isEmpty() &&
                statistics.dailyData.isEmpty() &&
                statistics.hourlyData.isEmpty() &&
                statistics.weekdayData.isEmpty() &&
                statistics.topCombos.isEmpty() &&
                statistics.topCustomers.isEmpty()
}
