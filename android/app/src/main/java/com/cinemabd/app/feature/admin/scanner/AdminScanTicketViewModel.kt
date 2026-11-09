package com.cinemabd.app.feature.admin.scanner

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.AdminRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class AdminScanTicketViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ScanTicketUiState())
    val uiState: StateFlow<ScanTicketUiState> = _uiState.asStateFlow()

    fun onCameraPermissionResult(granted: Boolean) {
        _uiState.update { it.copy(isCameraPermissionGranted = granted) }
    }

    fun toggleTorch() {
        _uiState.update { it.copy(isTorchEnabled = !it.isTorchEnabled) }
    }

    fun onQrScanned(qrText: String) {
        val trimmed = qrText.trim()
        if (trimmed.isBlank()) return

        // Prevent re-scanning while verifying
        if (_uiState.value.scanStatus is ScanStatus.Verifying) return

        _uiState.update { it.copy(scanStatus = ScanStatus.Verifying) }

        viewModelScope.launch {
            val result = adminRepository.checkIn(trimmed)
            result.onSuccess { apiResponse ->
                val data = apiResponse.data
                val message = apiResponse.message.ifBlank { data?.message.orEmpty() }

                val mappedStatus = if (apiResponse.success && data != null && data.success) {
                    ScanStatus.Valid(
                        data = data,
                        message = message.ifBlank { "Check-in vé thành công." }
                    )
                } else {
                    val isAlreadyCheckedIn = data?.isCheckedIn == true ||
                            message.contains("đã check-in", ignoreCase = true) ||
                            message.contains("đã soát", ignoreCase = true)
                    val isInvalid = message.contains("không đúng định dạng", ignoreCase = true) ||
                            message.contains("không hợp lệ", ignoreCase = true) ||
                            message.contains("chữ ký", ignoreCase = true) ||
                            message.contains("giả mạo", ignoreCase = true)
                    val isNotFound = message.contains("không tìm thấy", ignoreCase = true)

                    when {
                        isAlreadyCheckedIn -> ScanStatus.AlreadyUsed(
                            data = data,
                            message = message.ifBlank { "Vé đã check-in trước đó." }
                        )
                        isInvalid -> ScanStatus.InvalidOrTampered(
                            message = message.ifBlank { "Mã QR không hợp lệ hoặc bị giả mạo." }
                        )
                        isNotFound -> ScanStatus.NotFound(
                            message = message.ifBlank { "Không tìm thấy vé." }
                        )
                        else -> ScanStatus.Error(
                            message = message.ifBlank { "Soát vé thất bại." }
                        )
                    }
                }

                _uiState.update { it.copy(scanStatus = mappedStatus) }
            }.onFailure { throwable ->
                val errorMsg = if (throwable is SecurityException ||
                    throwable.message == "Tài khoản không có quyền soát vé"
                ) {
                    "Tài khoản không có quyền soát vé"
                } else {
                    throwable.toUserFriendlyMessage("Không thể kết nối máy chủ. Vui lòng thử lại.")
                }
                _uiState.update { it.copy(scanStatus = ScanStatus.Error(errorMsg)) }
            }
        }
    }

    fun resetScanner() {
        _uiState.update { it.copy(scanStatus = ScanStatus.Idle) }
    }
}
