package com.cinemabd.app.feature.auth

import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()

    fun onUsernameChange(username: String) {
        _uiState.update { it.copy(username = username, errorMessage = null) }
    }

    fun onPasswordChange(password: String) {
        _uiState.update { it.copy(password = password, errorMessage = null) }
    }

    fun onFullNameChange(fullName: String) {
        _uiState.update { it.copy(fullName = fullName, errorMessage = null) }
    }

    fun onEmailChange(email: String) {
        _uiState.update { it.copy(email = email, errorMessage = null) }
    }

    fun onPhoneNumberChange(phoneNumber: String) {
        _uiState.update { it.copy(phoneNumber = phoneNumber, errorMessage = null) }
    }

    fun toggleAuthMode() {
        _uiState.update {
            it.copy(
                isLoginMode = !it.isLoginMode,
                errorMessage = null,
                password = "",
                fullName = "",
                email = "",
                phoneNumber = ""
            )
        }
    }

    fun submit() {
        val state = _uiState.value

        if (state.isLoginMode) {
            if (state.username.isBlank()) {
                _uiState.update { it.copy(errorMessage = "Vui lòng nhập tài khoản") }
                return
            }
            if (state.password.isBlank()) {
                _uiState.update { it.copy(errorMessage = "Vui lòng nhập mật khẩu") }
                return
            }

            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            viewModelScope.launch {
                val result = authRepository.login(LoginRequest(state.username, state.password))
                if (result.isSuccess) {
                    _uiState.update { it.copy(isLoading = false, isSuccess = true) }
                } else {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            errorMessage = result.exceptionOrNull()?.message ?: "Đã xảy ra lỗi"
                        )
                    }
                }
            }
        } else {
            if (state.fullName.isBlank()) {
                _uiState.update { it.copy(errorMessage = "Vui lòng nhập họ tên") }
                return
            }
            if (state.username.isBlank()) {
                _uiState.update { it.copy(errorMessage = "Vui lòng nhập tài khoản") }
                return
            }
            if (state.password.isBlank()) {
                _uiState.update { it.copy(errorMessage = "Vui lòng nhập mật khẩu") }
                return
            }
            if (state.password.length < 6) {
                _uiState.update { it.copy(errorMessage = "Mật khẩu phải từ 6 ký tự") }
                return
            }
            if (state.email.isNotBlank() && !Patterns.EMAIL_ADDRESS.matcher(state.email).matches()) {
                _uiState.update { it.copy(errorMessage = "Email không hợp lệ") }
                return
            }

            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            viewModelScope.launch {
                val request = RegisterRequest(
                    fullName = state.fullName,
                    username = state.username,
                    password = state.password,
                    email = state.email.ifBlank { null },
                    phoneNumber = state.phoneNumber.ifBlank { null }
                )
                val result = authRepository.register(request)
                if (result.isSuccess) {
                    _uiState.update { it.copy(isLoading = false, isSuccess = true) }
                } else {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            errorMessage = result.exceptionOrNull()?.message ?: "Đã xảy ra lỗi"
                        )
                    }
                }
            }
        }
    }

    fun resetSuccessState() {
        _uiState.update { it.copy(isSuccess = false) }
    }
}
