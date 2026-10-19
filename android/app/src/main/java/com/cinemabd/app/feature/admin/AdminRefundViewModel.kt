package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminRefundRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface AdminRefundUiState {
    data object Loading : AdminRefundUiState
    data object Empty : AdminRefundUiState
    data class Success(
        val refunds: List<AdminRefundRequest>,
        val currentStatus: String?
    ) : AdminRefundUiState
    data class Error(val message: String) : AdminRefundUiState
}

@HiltViewModel
class AdminRefundViewModel @Inject constructor(
    private val repository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<AdminRefundUiState>(AdminRefundUiState.Loading)
    val uiState: StateFlow<AdminRefundUiState> = _uiState.asStateFlow()

    private val _actionLoading = MutableStateFlow(false)
    val actionLoading: StateFlow<Boolean> = _actionLoading.asStateFlow()

    private val _userMessage = MutableStateFlow<String?>(null)
    val userMessage: StateFlow<String?> = _userMessage.asStateFlow()

    private var selectedStatus: String? = null // null: All, "Pending", "Approved", "Rejected"

    init {
        loadRefunds()
    }

    fun setStatusFilter(status: String?) {
        selectedStatus = status
        loadRefunds()
    }

    fun loadRefunds() {
        viewModelScope.launch {
            _uiState.value = AdminRefundUiState.Loading
            repository.getRefunds(selectedStatus)
                .onSuccess { response ->
                    if (!response.success || response.data == null) {
                        _uiState.value = AdminRefundUiState.Error(
                            response.message.ifBlank { "Không thể tải danh sách hoàn tiền." }
                        )
                    } else if (response.data.isEmpty()) {
                        _uiState.value = AdminRefundUiState.Empty
                    } else {
                        _uiState.value = AdminRefundUiState.Success(response.data, selectedStatus)
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminRefundUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải danh sách hoàn tiền.")
                    )
                }
        }
    }

    fun approveRefund(id: Int, note: String?, onSuccess: () -> Unit) {
        viewModelScope.launch {
            _actionLoading.value = true
            repository.approveRefund(id, note?.trim()?.takeIf { it.isNotBlank() })
                .onSuccess { response ->
                    _actionLoading.value = false
                    if (response.success) {
                        _userMessage.value = "Đã duyệt hoàn tiền thành công."
                        onSuccess()
                        loadRefunds()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Duyệt hoàn tiền thất bại." }
                    }
                }
                .onFailure { error ->
                    _actionLoading.value = false
                    _userMessage.value = error.toUserFriendlyMessage("Không thể duyệt hoàn tiền.")
                }
        }
    }

    fun rejectRefund(id: Int, note: String?, onSuccess: () -> Unit) {
        viewModelScope.launch {
            _actionLoading.value = true
            repository.rejectRefund(id, note?.trim()?.takeIf { it.isNotBlank() })
                .onSuccess { response ->
                    _actionLoading.value = false
                    if (response.success) {
                        _userMessage.value = "Đã từ chối hoàn tiền."
                        onSuccess()
                        loadRefunds()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Từ chối hoàn tiền thất bại." }
                    }
                }
                .onFailure { error ->
                    _actionLoading.value = false
                    _userMessage.value = error.toUserFriendlyMessage("Không thể từ chối hoàn tiền.")
                }
        }
    }

    fun clearUserMessage() {
        _userMessage.value = null
    }
}
