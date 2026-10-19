package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminCombo
import com.cinemabd.app.feature.admin.model.AdminComboRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface AdminComboUiState {
    data object Loading : AdminComboUiState
    data object Empty : AdminComboUiState
    data class Success(val combos: List<AdminCombo>) : AdminComboUiState
    data class Error(val message: String) : AdminComboUiState
}

@HiltViewModel
class AdminComboViewModel @Inject constructor(
    private val repository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<AdminComboUiState>(AdminComboUiState.Loading)
    val uiState: StateFlow<AdminComboUiState> = _uiState.asStateFlow()

    private val _actionLoading = MutableStateFlow(false)
    val actionLoading: StateFlow<Boolean> = _actionLoading.asStateFlow()

    private val _userMessage = MutableStateFlow<String?>(null)
    val userMessage: StateFlow<String?> = _userMessage.asStateFlow()

    init {
        loadCombos()
    }

    fun loadCombos() {
        viewModelScope.launch {
            _uiState.value = AdminComboUiState.Loading
            repository.getCombos()
                .onSuccess { response ->
                    if (!response.success || response.data == null) {
                        _uiState.value = AdminComboUiState.Error(
                            response.message.ifBlank { "Không thể tải danh sách combo." }
                        )
                    } else if (response.data.isEmpty()) {
                        _uiState.value = AdminComboUiState.Empty
                    } else {
                        _uiState.value = AdminComboUiState.Success(response.data)
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminComboUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải danh sách combo.")
                    )
                }
        }
    }

    fun saveCombo(
        id: String,
        name: String,
        price: Double,
        description: String?,
        imageUrl: String?,
        isEdit: Boolean,
        onSuccess: () -> Unit
    ) {
        viewModelScope.launch {
            _actionLoading.value = true
            val request = AdminComboRequest(
                id = id.trim(),
                name = name.trim(),
                price = price,
                description = description?.trim()?.takeIf { it.isNotBlank() },
                imageUrl = imageUrl?.trim()?.takeIf { it.isNotBlank() }
            )

            val result = if (isEdit) {
                repository.updateCombo(id.trim(), request)
            } else {
                repository.createCombo(request)
            }

            _actionLoading.value = false
            result
                .onSuccess { response ->
                    if (response.success) {
                        _userMessage.value = if (isEdit) "Đã cập nhật combo thành công." else "Đã thêm combo mới thành công."
                        onSuccess()
                        loadCombos()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Lưu combo thất bại." }
                    }
                }
                .onFailure { error ->
                    _userMessage.value = error.toUserFriendlyMessage("Không thể lưu thông tin combo.")
                }
        }
    }

    fun deleteCombo(id: String, onSuccess: () -> Unit) {
        viewModelScope.launch {
            _actionLoading.value = true
            repository.deleteCombo(id)
                .onSuccess { response ->
                    _actionLoading.value = false
                    if (response.success) {
                        _userMessage.value = "Đã xóa combo thành công."
                        onSuccess()
                        loadCombos()
                    } else {
                        _userMessage.value = response.message.ifBlank { "Xóa combo thất bại." }
                    }
                }
                .onFailure { error ->
                    _actionLoading.value = false
                    _userMessage.value = error.toUserFriendlyMessage("Không thể xóa combo.")
                }
        }
    }

    fun clearUserMessage() {
        _userMessage.value = null
    }
}
