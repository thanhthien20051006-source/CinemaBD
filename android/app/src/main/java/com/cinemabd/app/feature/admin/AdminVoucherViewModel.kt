package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminVoucher
import com.cinemabd.app.feature.admin.model.AdminVoucherRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.UUID
import javax.inject.Inject

sealed interface AdminVoucherUiState {
    data object Loading : AdminVoucherUiState
    data object Empty : AdminVoucherUiState
    data class Success(val vouchers: List<AdminVoucher>, val query: String) : AdminVoucherUiState
    data class Error(val message: String) : AdminVoucherUiState
}

@HiltViewModel
class AdminVoucherViewModel @Inject constructor(
    private val repository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<AdminVoucherUiState>(AdminVoucherUiState.Loading)
    val uiState: StateFlow<AdminVoucherUiState> = _uiState.asStateFlow()

    private val _actionLoading = MutableStateFlow(false)
    val actionLoading: StateFlow<Boolean> = _actionLoading.asStateFlow()

    private val _userMessage = MutableStateFlow<String?>(null)
    val userMessage: StateFlow<String?> = _userMessage.asStateFlow()

    private var searchJob: Job? = null
    private var currentQuery = ""

    init {
        loadVouchers()
    }

    fun onSearchChange(query: String) {
        currentQuery = query
        searchJob?.cancel()
        searchJob = viewModelScope.launch {
            loadVouchers()
        }
    }

    fun loadVouchers() {
        viewModelScope.launch {
            _uiState.value = AdminVoucherUiState.Loading
            repository.getVouchers(currentQuery.trim())
                .onSuccess { response ->
                    if (!response.success || response.data == null) {
                        _uiState.value = AdminVoucherUiState.Error(
                            response.message.ifBlank { "Không thể tải danh sách voucher." }
                        )
                    } else {
                        val filtered = response.data.filter {
                            currentQuery.isBlank() ||
                                it.code.contains(currentQuery, ignoreCase = true) ||
                                (it.description?.contains(currentQuery, ignoreCase = true) == true)
                        }
                        _uiState.value = if (filtered.isEmpty()) {
                            AdminVoucherUiState.Empty
                        } else {
                            AdminVoucherUiState.Success(filtered, currentQuery)
                        }
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminVoucherUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải danh sách voucher.")
                    )
                }
        }
    }

    fun createVoucher(
        code: String,
        description: String?,
        expiredAt: String,
        discountValue: Double,
        discountType: String,
        minOrderAmount: Double,
        maxDiscountAmount: Double,
        onSuccess: () -> Unit
    ) {
        viewModelScope.launch {
            _actionLoading.value = true
            val voucherId = "VOUCHER_" + UUID.randomUUID().toString().take(8).uppercase()
            val request = AdminVoucherRequest(
                id = voucherId,
                customerId = "ALL",
                isGlobal = true,
                code = code.trim().uppercase(),
                description = description?.trim()?.takeIf { it.isNotBlank() },
                expiredAt = expiredAt.trim(),
                discountValue = discountValue,
                discountType = discountType,
                minOrderAmount = minOrderAmount,
                maxDiscountAmount = maxDiscountAmount
            )

            repository.createVoucher(request)
                .onSuccess { response ->
                    _actionLoading.value = false
                    if (response.success) {
                        _userMessage.value = "Đã tạo voucher mới thành công."
                        onSuccess()
                        loadVouchers()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Tạo voucher thất bại." }
                    }
                }
                .onFailure { error ->
                    _actionLoading.value = false
                    _userMessage.value = error.toUserFriendlyMessage("Không thể tạo voucher.")
                }
        }
    }

    fun deleteVoucher(id: String, onSuccess: () -> Unit) {
        viewModelScope.launch {
            _actionLoading.value = true
            repository.deleteVoucher(id)
                .onSuccess { response ->
                    _actionLoading.value = false
                    if (response.success) {
                        _userMessage.value = "Đã vô hiệu hóa voucher thành công."
                        onSuccess()
                        loadVouchers()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Vô hiệu hóa voucher thất bại." }
                    }
                }
                .onFailure { error ->
                    _actionLoading.value = false
                    _userMessage.value = error.toUserFriendlyMessage("Không thể vô hiệu hóa voucher.")
                }
        }
    }

    fun clearUserMessage() {
        _userMessage.value = null
    }
}
