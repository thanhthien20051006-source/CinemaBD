package com.cinemabd.app.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class CheckInViewModel @Inject constructor(
    private val adminRepository: AdminRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(CheckInUiState())
    val uiState: StateFlow<CheckInUiState> = _uiState.asStateFlow()

    fun onQrInputChange(text: String) {
        _uiState.update { it.copy(qrInput = text, cameraPermissionDenied = false) }
    }

    fun onCameraPermissionResult(isGranted: Boolean) {
        _uiState.update { it.copy(cameraPermissionDenied = !isGranted) }
    }

    fun submitCheckIn(qrTextOverride: String? = null) {
        val qr = (qrTextOverride ?: _uiState.value.qrInput).trim()
        if (qr.isBlank()) {
            _uiState.update {
                it.copy(
                    resultState = CheckInResultState.InvalidQr("Vui lòng nhập hoặc quét mã QR.")
                )
            }
            return
        }

        _uiState.update {
            it.copy(
                qrInput = qr,
                isLoading = true,
                resultState = null,
                cameraPermissionDenied = false
            )
        }

        viewModelScope.launch {
            val result = adminRepository.checkIn(qr)
            result.onSuccess { apiResponse ->
                val data = apiResponse.data
                val message = apiResponse.message.ifBlank { data?.message.orEmpty() }

                val mappedState = if (apiResponse.success && data != null && data.success) {
                    CheckInResultState.Success(
                        ticketId = data.ticketId,
                        movieTitle = data.movieTitle,
                        seatId = data.seatId,
                        customerName = data.customerName,
                        message = message.ifBlank { "Check-in vé thành công." }
                    )
                } else {
                    val isAlreadyCheckedIn = data?.isCheckedIn == true ||
                            message.contains("đã check-in", ignoreCase = true) ||
                            message.contains("đã soát", ignoreCase = true)
                    val isInvalid = message.contains("không đúng định dạng", ignoreCase = true) ||
                            message.contains("không hợp lệ", ignoreCase = true)
                    val isNotFound = message.contains("không tìm thấy", ignoreCase = true)

                    when {
                        isAlreadyCheckedIn -> CheckInResultState.AlreadyCheckedIn(
                            ticketId = data?.ticketId,
                            movieTitle = data?.movieTitle,
                            seatId = data?.seatId,
                            message = message.ifBlank { "Vé đã check-in trước đó." }
                        )
                        isInvalid -> CheckInResultState.InvalidQr(
                            message = message.ifBlank { "Mã QR không hợp lệ." }
                        )
                        isNotFound -> CheckInResultState.NotFound(
                            message = message.ifBlank { "Không tìm thấy vé." }
                        )
                        else -> CheckInResultState.Error(
                            message = message.ifBlank { "Soát vé thất bại." }
                        )
                    }
                }

                _uiState.update {
                    it.copy(
                        isLoading = false,
                        resultState = mappedState
                    )
                }
            }.onFailure { throwable ->
                val errorMsg = if (throwable is SecurityException || throwable.message == "Tài khoản không có quyền soát vé") {
                    "Tài khoản không có quyền soát vé"
                } else {
                    throwable.toUserFriendlyMessage("Không thể kết nối máy chủ. Vui lòng thử lại.")
                }
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        resultState = CheckInResultState.Error(errorMsg)
                    )
                }
            }
        }
    }
}
