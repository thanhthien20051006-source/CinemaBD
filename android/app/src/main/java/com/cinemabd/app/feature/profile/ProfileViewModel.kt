package com.cinemabd.app.feature.profile

import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.UpdateCustomerProfileRequest
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.data.repository.ProfileRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ProfileViewModel @Inject constructor(
    private val repository: ProfileRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ProfileUiState(isLoading = true))
    val uiState: StateFlow<ProfileUiState> = _uiState.asStateFlow()

    init {
        repository.isDarkThemeFlow
            .onEach { isDark ->
                _uiState.update { it.copy(isDarkMode = isDark) }
            }
            .launchIn(viewModelScope)

        loadProfile()
    }

    fun loadProfile() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            val profileResult = repository.getProfile()
            val loyaltyResult = repository.getLoyaltyPoints()

            profileResult.fold(
                onSuccess = { profile ->
                    val loyalty = loyaltyResult.getOrDefault(0)
                    _uiState.update { current ->
                        current.copy(
                            isLoading = false,
                            profile = profile,
                            loyaltyPoints = loyalty,
                            editFullName = profile.fullName,
                            editEmail = profile.email.orEmpty(),
                            editPhoneNumber = profile.phoneNumber.orEmpty(),
                            editBirthDate = profile.birthDate?.substringBefore('T').orEmpty(),
                            errorMessage = null
                        )
                    }
                },
                onFailure = { error ->
                    _uiState.update { current ->
                        current.copy(
                            isLoading = false,
                            errorMessage = error.toUserFriendlyMessage("Không thể tải thông tin tài khoản")
                        )
                    }
                }
            )
        }
    }

    fun startEdit() {
        val currentProfile = _uiState.value.profile ?: return
        _uiState.update {
            it.copy(
                isEditMode = true,
                editFullName = currentProfile.fullName,
                editEmail = currentProfile.email.orEmpty(),
                editPhoneNumber = currentProfile.phoneNumber.orEmpty(),
                editBirthDate = currentProfile.birthDate?.substringBefore('T').orEmpty(),
                fullNameError = null,
                emailError = null,
                phoneError = null,
                errorMessage = null,
                successMessage = null
            )
        }
    }

    fun cancelEdit() {
        val currentProfile = _uiState.value.profile
        _uiState.update {
            it.copy(
                isEditMode = false,
                editFullName = currentProfile?.fullName.orEmpty(),
                editEmail = currentProfile?.email.orEmpty(),
                editPhoneNumber = currentProfile?.phoneNumber.orEmpty(),
                editBirthDate = currentProfile?.birthDate?.substringBefore('T').orEmpty(),
                fullNameError = null,
                emailError = null,
                phoneError = null
            )
        }
    }

    fun onFullNameChange(value: String) {
        _uiState.update { it.copy(editFullName = value, fullNameError = null) }
    }

    fun onEmailChange(value: String) {
        _uiState.update { it.copy(editEmail = value, emailError = null) }
    }

    fun onPhoneNumberChange(value: String) {
        _uiState.update { it.copy(editPhoneNumber = value, phoneError = null) }
    }

    fun onBirthDateChange(value: String) {
        _uiState.update { it.copy(editBirthDate = value) }
    }

    fun saveProfile() {
        val state = _uiState.value
        val fullName = state.editFullName.trim()
        val email = state.editEmail.trim()
        val phone = state.editPhoneNumber.trim()
        val birthDate = state.editBirthDate.trim().ifBlank { null }

        var hasError = false
        var nameErr: String? = null
        var emailErr: String? = null
        var phoneErr: String? = null

        if (fullName.isBlank()) {
            nameErr = "Họ tên không được để trống"
            hasError = true
        }

        if (email.isNotBlank() && !Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            emailErr = "Email không đúng định dạng"
            hasError = true
        }

        if (phone.isNotBlank() && !phone.matches(Regex("^0[0-9]{9,10}\$"))) {
            phoneErr = "Số điện thoại không hợp lệ (cần 10 chữ số)"
            hasError = true
        }

        if (hasError) {
            _uiState.update {
                it.copy(
                    fullNameError = nameErr,
                    emailError = emailErr,
                    phoneError = phoneErr
                )
            }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, errorMessage = null, successMessage = null) }
            val request = UpdateCustomerProfileRequest(
                fullName = fullName,
                email = email.ifBlank { null },
                phoneNumber = phone.ifBlank { null },
                birthDate = birthDate
            )

            val result = repository.updateProfile(request)
            result.fold(
                onSuccess = { updatedProfile ->
                    _uiState.update {
                        it.copy(
                            isSaving = false,
                            isEditMode = false,
                            profile = updatedProfile,
                            editFullName = updatedProfile.fullName,
                            editEmail = updatedProfile.email.orEmpty(),
                            editPhoneNumber = updatedProfile.phoneNumber.orEmpty(),
                            editBirthDate = updatedProfile.birthDate?.substringBefore('T').orEmpty(),
                            successMessage = "Cập nhật thông tin cá nhân thành công!"
                        )
                    }
                },
                onFailure = { error ->
                    _uiState.update {
                        it.copy(
                            isSaving = false,
                            errorMessage = error.toUserFriendlyMessage("Cập nhật không thành công. Vui lòng thử lại.")
                        )
                    }
                }
            )
        }
    }

    fun setDarkMode(isDark: Boolean) {
        viewModelScope.launch {
            repository.setDarkTheme(isDark)
        }
    }

    fun openLogoutDialog() {
        _uiState.update { it.copy(showLogoutDialog = true) }
    }

    fun dismissLogoutDialog() {
        _uiState.update { it.copy(showLogoutDialog = false) }
    }

    fun logout() {
        viewModelScope.launch {
            _uiState.update { it.copy(showLogoutDialog = false) }
            repository.logout()
            _uiState.update { it.copy(isLoggedOut = true) }
        }
    }

    fun clearMessages() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }
}
