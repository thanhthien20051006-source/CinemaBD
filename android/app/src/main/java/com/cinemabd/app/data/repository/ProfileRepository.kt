package com.cinemabd.app.data.repository

import com.cinemabd.app.core.database.CinemaDatabase
import com.cinemabd.app.core.datastore.ThemePreferences
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.network.AccountApi
import com.cinemabd.app.core.network.model.CustomerProfileResponse
import com.cinemabd.app.core.network.model.UpdateCustomerProfileRequest
import com.cinemabd.app.feature.booking.BookingCartManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ProfileRepository @Inject constructor(
    private val accountApi: AccountApi,
    private val tokenDataStore: TokenDataStore,
    private val themePreferences: ThemePreferences,
    private val cinemaDatabase: CinemaDatabase,
    private val cartManager: BookingCartManager
) {

    val isDarkThemeFlow: Flow<Boolean> = themePreferences.isDarkThemeFlow

    suspend fun getProfile(): Result<CustomerProfileResponse> = withContext(Dispatchers.IO) {
        try {
            val response = accountApi.getProfile()
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tải thông tin tài khoản" }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun updateProfile(request: UpdateCustomerProfileRequest): Result<CustomerProfileResponse> =
        withContext(Dispatchers.IO) {
            try {
                val response = accountApi.updateProfile(request)
                if (response.success && response.data != null) {
                    Result.success(response.data)
                } else {
                    Result.failure(Exception(response.message.ifBlank { "Không thể cập nhật thông tin tài khoản" }))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun getLoyaltyPoints(): Result<Int> = withContext(Dispatchers.IO) {
        try {
            val response = accountApi.getLoyalty()
            if (response.success && response.data != null) {
                Result.success(response.data.balance)
            } else {
                Result.success(0)
            }
        } catch (e: Exception) {
            Result.success(0)
        }
    }

    suspend fun setDarkTheme(isDark: Boolean) {
        themePreferences.setDarkTheme(isDark)
    }

    suspend fun logout() = withContext(Dispatchers.IO) {
        tokenDataStore.clearToken()
        cinemaDatabase.clearAllTables()
        cartManager.clearCart()
    }
}
