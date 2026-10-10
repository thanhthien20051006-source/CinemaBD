package com.cinemabd.app.data.repository

import com.cinemabd.app.core.network.CombosApi
import com.cinemabd.app.core.network.model.ComboResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class CombosRepository @Inject constructor(
    private val combosApi: CombosApi
) {
    suspend fun getCombos(): Result<List<ComboResponse>> = withContext(Dispatchers.IO) {
        try {
            val response = combosApi.getCombos()
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tải danh sách combo." }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getComboById(id: String): Result<ComboResponse> = withContext(Dispatchers.IO) {
        try {
            val response = combosApi.getComboById(id)
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tải thông tin combo." }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
