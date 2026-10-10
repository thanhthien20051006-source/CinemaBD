package com.cinemabd.app.feature.auth

import kotlinx.serialization.Serializable

@Serializable
data class LoginRequest(
    val username: String,
    val password: String
)

@Serializable
data class RegisterRequest(
    val fullName: String,
    val username: String,
    val password: String,
    val email: String? = null,
    val phoneNumber: String? = null
)

@Serializable
data class AuthResponse(
    val userId: String,
    val username: String,
    val fullName: String,
    val token: String
)
