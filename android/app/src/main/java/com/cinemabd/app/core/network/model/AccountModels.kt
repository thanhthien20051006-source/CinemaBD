package com.cinemabd.app.core.network.model

import kotlinx.serialization.Serializable

@Serializable
data class CustomerProfileResponse(
    val userId: String = "",
    val username: String = "",
    val fullName: String = "",
    val email: String? = null,
    val phoneNumber: String? = null,
    val birthDate: String? = null,
    val totalSpent: Double = 0.0
)

@Serializable
data class UpdateCustomerProfileRequest(
    val fullName: String,
    val email: String? = null,
    val phoneNumber: String? = null,
    val birthDate: String? = null
)

@Serializable
data class LoyaltyPointResponse(
    val balance: Int = 0,
    val rewardPoints: Int = 0,
    val earnedPoints: Int = 0,
    val usedPoints: Int = 0,
    val pointValue: Int = 1000
)
