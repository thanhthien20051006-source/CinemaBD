package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.CheckInRequest
import com.cinemabd.app.feature.admin.model.CheckInResultData
import retrofit2.HttpException
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AdminRepository @Inject constructor(
    private val adminApi: AdminApi
) {
    suspend fun checkIn(qrText: String): Result<ApiResponse<CheckInResultData>> {
        return try {
            val response = adminApi.checkIn(CheckInRequest(qrText.trim()))
            Result.success(response)
        } catch (e: HttpException) {
            if (e.code() == 403) {
                Result.failure(SecurityException("Tài khoản không có quyền soát vé"))
            } else {
                Result.failure(e)
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
