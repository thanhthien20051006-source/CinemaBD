package com.cinemabd.app.feature.auth

data class AuthUiState(
    val isLoginMode: Boolean = true,
    val username: String = "",
    val password: String = "",
    val fullName: String = "",
    val email: String = "",
    val phoneNumber: String = "",
    val isLoading: Boolean = false,
    val isSuccess: Boolean = false,
    val role: String? = null,
    val requiresPasswordChange: Boolean = false,
    val passwordChangeToken: String? = null,
    val passwordChangeForAdmin: Boolean = false,
    val passwordChangeCompleted: Boolean = false,
    val currentPassword: String = "",
    val newPassword: String = "",
    val confirmPassword: String = "",
    val errorMessage: String? = null
)
