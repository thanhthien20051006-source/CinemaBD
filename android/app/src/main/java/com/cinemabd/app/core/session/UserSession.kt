package com.cinemabd.app.core.session

data class UserSession(
    val accessToken: String,
    val refreshToken: String? = null,
    val userId: String? = null,
    val userName: String? = null,
    val email: String? = null,
    val role: UserRole,
    val expiresAtUtcMillis: Long
)

enum class UserRole(val value: String) {
    Customer("Customer"),
    Admin("Admin");

    companion object {
        fun from(value: String?): UserRole? = when (value?.trim()?.lowercase()) {
            "customer" -> Customer
            "admin" -> Admin
            else -> null
        }
    }
}

sealed interface AuthState {
    data object Loading : AuthState
    data class AuthenticatedCustomer(val session: UserSession) : AuthState
    data class AuthenticatedAdmin(val session: UserSession) : AuthState
    data object Unauthenticated : AuthState
}
