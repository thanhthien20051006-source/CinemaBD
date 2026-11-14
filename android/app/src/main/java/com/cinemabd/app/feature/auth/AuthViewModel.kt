package com.cinemabd.app.feature.auth

import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.io.IOException
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

    fun onCurrentPasswordChange(password: String) {
        _uiState.update { it.copy(currentPassword = password, errorMessage = null) }
    }

    fun onNewPasswordChange(password: String) {
        _uiState.update { it.copy(newPassword = password, errorMessage = null) }
    }

    fun onConfirmPasswordChange(password: String) {
        _uiState.update { it.copy(confirmPassword = password, errorMessage = null) }
    }

    fun onMfaCodeChange(code: String) {
        _uiState.update { it.copy(mfaCode = code, errorMessage = null) }
    }

    fun onToggleRecoveryMode(isRecovery: Boolean) {
        _uiState.update { it.copy(isRecoveryCodeMode = isRecovery, mfaCode = "", errorMessage = null) }
    }

    fun submitMfaVerify() {
        val state = _uiState.value
        val challenge = state.mfaChallenge
        if (challenge.isNullOrBlank()) {
            _uiState.update { it.copy(errorMessage = "Phiên xác thực đã hết hạn. Vui lòng đăng nhập lại.") }
            return
        }
        if (state.mfaCode.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Vui lòng nhập mã xác minh.") }
            return
        }

        _uiState.update { it.copy(isLoading = true, errorMessage = null) }
        viewModelScope.launch {
            val result = authRepository.adminMfaVerify(
                AdminMfaVerifyRequest(
                    mfaChallenge = challenge,
                    code = state.mfaCode.trim(),
                    isRecoveryCode = state.isRecoveryCodeMode
                )
            )
            if (result.isSuccess) {
                val adminData = result.getOrNull()
                val adminRole = adminData?.role?.takeIf { it.isNotBlank() } ?: "Admin"
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        isSuccess = !(adminData?.mustChangePassword == true || adminData?.passwordExpired == true),
                        requiresPasswordChange = adminData?.mustChangePassword == true || adminData?.passwordExpired == true,
                        passwordChangeToken = adminData?.token.takeIf { adminData?.mustChangePassword == true || adminData?.passwordExpired == true },
                        passwordChangeForAdmin = adminData?.mustChangePassword == true || adminData?.passwordExpired == true,
                        role = adminRole,
                        isMfaRequired = false,
                        mfaChallenge = null,
                        mfaCode = ""
                    )
                }
            } else {
                val exception = result.exceptionOrNull()
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = exception.toUserFriendlyMessage("Mã xác minh không chính xác hoặc phiên đã hết hạn.")
                    )
                }
            }
        }
    }

    fun submitPasswordChange() {
        val state = _uiState.value
        if (state.currentPassword.isBlank() || state.newPassword.isBlank() || state.confirmPassword.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Vui lòng nhập đầy đủ thông tin mật khẩu.") }
            return
        }
        if (state.newPassword.length < 12 || state.newPassword.none(Char::isUpperCase)
            || state.newPassword.none(Char::isLowerCase) || state.newPassword.none(Char::isDigit)
            || state.newPassword.all(Char::isLetterOrDigit)) {
            _uiState.update { it.copy(errorMessage = "Mật khẩu mới cần tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.") }
            return
        }
        if (state.newPassword != state.confirmPassword) {
            _uiState.update { it.copy(errorMessage = "Mật khẩu xác nhận không khớp.") }
            return
        }

        _uiState.update { it.copy(isLoading = true, errorMessage = null) }
        viewModelScope.launch {
            val request = ChangePasswordRequest(state.currentPassword, state.newPassword)
            val result = if (state.passwordChangeForAdmin) {
                authRepository.changeAdminPassword(state.passwordChangeToken, request)
            } else {
                authRepository.changeCustomerPassword(state.passwordChangeToken, request)
            }
            if (result.isSuccess) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        requiresPasswordChange = false,
                        passwordChangeCompleted = true,
                        currentPassword = "",
                        newPassword = "",
                        confirmPassword = "",
                        password = ""
                    )
                }
            } else {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = result.exceptionOrNull().toUserFriendlyMessage("Không đổi được mật khẩu. Vui lòng thử lại.")
                    )
                }
            }
        }
    }

    fun resetPasswordChangeCompleted() {
        _uiState.update {
            it.copy(
                passwordChangeCompleted = false,
                requiresPasswordChange = false,
                passwordChangeToken = null,
                passwordChangeForAdmin = false,
                currentPassword = "",
                newPassword = "",
                confirmPassword = "",
                password = "",
                isLoginMode = true
            )
        }
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

    private fun isNetworkError(throwable: Throwable?): Boolean {
        var current: Throwable? = throwable
        while (current != null) {
            if (current is IOException) return true
            current = current.cause
        }
        return false
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
                _uiState.update {
                    it.copy(
                        requiresPasswordChange = false,
                        passwordChangeToken = null,
                        passwordChangeForAdmin = false,
                        passwordChangeCompleted = false
                    )
                }
                val customerResult = authRepository.login(LoginRequest(state.username, state.password))
                if (customerResult.isSuccess) {
                    val authData = customerResult.getOrNull()
                    val role = authData?.role ?: "Customer"
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isSuccess = authData?.requiresPasswordChange != true,
                            requiresPasswordChange = authData?.requiresPasswordChange == true,
                            passwordChangeToken = authData?.token.takeIf { authData?.requiresPasswordChange == true },
                            passwordChangeForAdmin = false,
                            role = role
                        )
                    }
                    return@launch
                }

                val customerException = customerResult.exceptionOrNull()
                if (isNetworkError(customerException)) {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            errorMessage = customerException.toUserFriendlyMessage("Không thể kết nối máy chủ. Vui lòng kiểm tra lại kết nối mạng.")
                        )
                    }
                    return@launch
                }

                // Lỗi nghiệp vụ (400 hoặc tài khoản không tồn tại / sai mật khẩu) -> gọi admin login
                val adminResult = authRepository.adminLogin(LoginRequest(state.username, state.password))
                if (adminResult.isSuccess) {
                    val adminData = adminResult.getOrNull()
                    if (adminData?.mfaRequired == true) {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                isSuccess = false,
                                isMfaRequired = true,
                                mfaChallenge = adminData.mfaChallenge,
                                mfaCode = "",
                                role = "Admin"
                            )
                        }
                        return@launch
                    }
                    val adminRole = adminData?.role?.takeIf { it.isNotBlank() } ?: "Admin"
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isSuccess = !(adminData?.mustChangePassword == true || adminData?.passwordExpired == true),
                            requiresPasswordChange = adminData?.mustChangePassword == true || adminData?.passwordExpired == true,
                            passwordChangeToken = adminData?.token.takeIf { adminData?.mustChangePassword == true || adminData?.passwordExpired == true },
                            passwordChangeForAdmin = adminData?.mustChangePassword == true || adminData?.passwordExpired == true,
                            role = adminRole
                        )
                    }
                } else {
                    val adminException = adminResult.exceptionOrNull()
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            errorMessage = adminException.toUserFriendlyMessage("Đăng nhập thất bại. Vui lòng kiểm tra lại tài khoản hoặc mật khẩu.")
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
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isSuccess = true,
                            role = result.getOrNull()?.role ?: "Customer"
                        )
                    }
                } else {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            errorMessage = result.exceptionOrNull().toUserFriendlyMessage("Đăng ký thất bại. Vui lòng thử lại.")
                        )
                    }
                }
            }
        }
    }

    fun resetSuccessState() {
        _uiState.update { it.copy(isSuccess = false, role = null) }
    }
}

typealias LoginViewModel = AuthViewModel
