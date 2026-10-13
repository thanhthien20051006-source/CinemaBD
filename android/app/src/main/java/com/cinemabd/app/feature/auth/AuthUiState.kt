package com.cinemabd.app.feature.auth

data class AuthUiState(
    val isLoginMode: Boolean = true,
    val isAdminMode: Boolean = false,
    val username: String = "",
    val password: String = "",
    val fullName: String = "",
    val email: String = "",
    val phoneNumber: String = "",
    val isLoading: Boolean = false,
    val isSuccess: Boolean = false,
    val errorMessage: String? = null
)
