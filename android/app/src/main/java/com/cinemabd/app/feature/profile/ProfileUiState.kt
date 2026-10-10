package com.cinemabd.app.feature.profile

import com.cinemabd.app.core.network.model.CustomerProfileResponse

data class ProfileUiState(
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val isEditMode: Boolean = false,
    val profile: CustomerProfileResponse? = null,
    val loyaltyPoints: Int? = null,
    // Edit Form
    val editFullName: String = "",
    val editEmail: String = "",
    val editPhoneNumber: String = "",
    val editBirthDate: String = "",
    // Field Errors
    val fullNameError: String? = null,
    val emailError: String? = null,
    val phoneError: String? = null,
    // Global Messages
    val errorMessage: String? = null,
    val successMessage: String? = null,
    // Theme & Logout
    val isDarkMode: Boolean = true,
    val showLogoutDialog: Boolean = false,
    val isLoggedOut: Boolean = false
)
