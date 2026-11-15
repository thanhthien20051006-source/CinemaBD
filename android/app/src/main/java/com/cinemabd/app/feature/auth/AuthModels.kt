package com.cinemabd.app.feature.auth

import kotlinx.serialization.Serializable

@Serializable
data class LoginRequest(
    val username: String,
    val password: String,
    val deviceName: String? = "Android app"
)

@Serializable
data class ChangePasswordRequest(
    val currentPassword: String,
    val newPassword: String
)

@Serializable
data class RegisterRequest(
    val fullName: String,
    val username: String,
    val password: String,
    val email: String? = null,
    val phoneNumber: String? = null,
    val deviceName: String? = "Android app"
)

@Serializable
data class AuthResponse(
    val userId: String,
    val username: String,
    val fullName: String,
    val token: String,
    val mustChangePassword: Boolean = false,
    val passwordExpired: Boolean = false,
    val email: String? = null,
    val refreshToken: String? = null,
    val role: String? = null
) {
    val requiresPasswordChange: Boolean
        get() = mustChangePassword || passwordExpired
}

@Serializable
data class AdminAuthResponse(
    val adminId: Int = 0,
    val username: String = "",
    val fullName: String = "",
    val role: String? = null,
    val token: String = "",
    val mustChangePassword: Boolean = false,
    val passwordExpired: Boolean = false,
    val refreshToken: String? = null,
    val sessionId: String? = null,
    val mfaRequired: Boolean = false,
    val mfaChallenge: String? = null
)

@Serializable
data class AdminMfaVerifyRequest(
    val mfaChallenge: String,
    val code: String,
    val isRecoveryCode: Boolean = false,
    val deviceName: String? = "Android app"
)
