package com.cinemabd.app.feature.tickets

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.database.entity.TicketDetailEntity
import com.cinemabd.app.core.database.entity.TicketEntity
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.data.repository.TicketsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

enum class TicketFilterTab {
    UPCOMING, // Vé sắp xem (Active/Valid)
    HISTORY   // Lịch sử vé đã dùng hoặc hết hạn (Used/Expired)
}

data class MyTicketsUiState(
    val isLoading: Boolean = false,
    val tickets: List<TicketEntity> = emptyList(),
    val selectedTab: TicketFilterTab = TicketFilterTab.UPCOMING,
    val selectedTicket: TicketEntity? = null,
    val selectedTicketDetails: List<TicketDetailEntity> = emptyList(),
    val selectedSeatIndex: Int = 0,
    val errorMessage: String? = null,
    val isOffline: Boolean = false
) {
    val filteredTickets: List<TicketEntity>
        get() = tickets.filter { ticket ->
            val isUpcoming = DateTimeUtils.isUpcoming(ticket.showDate, ticket.startTime)
            val isCheckedInOrUsed = (ticket.checkedInCount >= ticket.ticketCount && ticket.ticketCount > 0) ||
                    ticket.status?.contains("đã check-in", ignoreCase = true) == true ||
                    ticket.status?.contains("checked", ignoreCase = true) == true ||
                    ticket.status?.contains("đã sử dụng", ignoreCase = true) == true ||
                    ticket.status?.contains("hết hạn", ignoreCase = true) == true ||
                    ticket.status?.contains("hủy", ignoreCase = true) == true ||
                    ticket.status?.contains("cancelled", ignoreCase = true) == true

            when (selectedTab) {
                TicketFilterTab.UPCOMING -> isUpcoming && !isCheckedInOrUsed
                TicketFilterTab.HISTORY -> !isUpcoming || isCheckedInOrUsed
            }
        }

    val upcomingCount: Int
        get() = tickets.count { ticket ->
            val isUpcoming = DateTimeUtils.isUpcoming(ticket.showDate, ticket.startTime)
            val isCheckedInOrUsed = (ticket.checkedInCount >= ticket.ticketCount && ticket.ticketCount > 0) ||
                    ticket.status?.contains("đã check-in", ignoreCase = true) == true ||
                    ticket.status?.contains("checked", ignoreCase = true) == true ||
                    ticket.status?.contains("đã sử dụng", ignoreCase = true) == true ||
                    ticket.status?.contains("hết hạn", ignoreCase = true) == true ||
                    ticket.status?.contains("hủy", ignoreCase = true) == true ||
                    ticket.status?.contains("cancelled", ignoreCase = true) == true
            isUpcoming && !isCheckedInOrUsed
        }

    val historyCount: Int
        get() = tickets.size - upcomingCount
}

@HiltViewModel
class MyTicketsViewModel @Inject constructor(
    private val ticketsRepository: TicketsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(MyTicketsUiState())
    val uiState: StateFlow<MyTicketsUiState> = _uiState.asStateFlow()

    init {
        observeTicketsFromDatabase()
        loadTickets(forceRefresh = false)
    }

    /**
     * Lắng nghe Flow danh sách vé từ Room Database để đảm bảo dữ liệu luôn khả dụng offline.
     */
    private fun observeTicketsFromDatabase() {
        viewModelScope.launch {
            ticketsRepository.getTicketsFlow().collectLatest { roomTickets ->
                _uiState.update { current ->
                    current.copy(
                        tickets = roomTickets,
                        isLoading = if (roomTickets.isNotEmpty()) false else current.isLoading
                    )
                }
            }
        }
    }

    /**
     * Tải vé: Ưu tiên hiển thị dữ liệu từ Room trước; nếu có mạng thì gọi API đồng bộ
     * thêm các vé mới nhất từ backend và upsert vào Room.
     */
    fun loadTickets(forceRefresh: Boolean = false) {
        viewModelScope.launch {
            if (forceRefresh || _uiState.value.tickets.isEmpty()) {
                _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            }

            // Đồng bộ cả chi tiết vé có mã QR (HMAC) lẫn danh sách lịch sử đặt vé
            try {
                // 1. Lấy danh sách vé kèm mã QR ký số HMAC
                ticketsRepository.getMyTicketPayloads()
                // 2. Lấy thông tin lịch sử hóa đơn tổng quan
                val result = ticketsRepository.getTickets()

                result.onSuccess { data ->
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isOffline = data.isOffline,
                            errorMessage = null
                        )
                    }
                }.onFailure { throwable ->
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isOffline = true,
                            errorMessage = if (it.tickets.isEmpty()) {
                                throwable.toUserFriendlyMessage("Không thể tải danh sách vé. Vui lòng thử lại.")
                            } else null
                        )
                    }
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        isOffline = true,
                        errorMessage = if (it.tickets.isEmpty()) {
                            e.toUserFriendlyMessage("Không thể kết nối đến máy chủ.")
                        } else null
                    )
                }
            }
        }
    }

    fun onSelectTab(tab: TicketFilterTab) {
        _uiState.update { it.copy(selectedTab = tab) }
    }

    fun onSelectTicket(ticket: TicketEntity) {
        viewModelScope.launch {
            // Tải chi tiết các vé (từng ghế) của hóa đơn này từ Room để hỗ trợ chọn mã QR từng ghế
            val details = ticketsRepository.getTicketDetailsOnce(ticket.invoiceId)
            _uiState.update {
                it.copy(
                    selectedTicket = ticket,
                    selectedTicketDetails = details,
                    selectedSeatIndex = 0
                )
            }
        }
    }

    fun onSelectSeatIndex(index: Int) {
        _uiState.update { it.copy(selectedSeatIndex = index) }
    }

    fun onDismissDetail() {
        _uiState.update {
            it.copy(
                selectedTicket = null,
                selectedTicketDetails = emptyList(),
                selectedSeatIndex = 0
            )
        }
    }
}
