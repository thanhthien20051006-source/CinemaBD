package com.cinemabd.app.data.repository

import com.cinemabd.app.BuildConfig
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
                val mappedData = response.data.map { combo ->
                    combo.copy(
                        imageUrl = fixImageUrl(combo.imageUrl)
                    )
                }
                Result.success(mappedData)
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
                val combo = response.data.copy(
                    imageUrl = fixImageUrl(response.data.imageUrl)
                )
                Result.success(combo)
            } else {
                Result.failure(Exception(response.message.ifBlank { "Không thể tải thông tin combo." }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    private fun fixImageUrl(url: String?): String? {
        if (url.isNullOrBlank()) return null

        var fixedUrl = url
        if (fixedUrl.contains("localhost", ignoreCase = true)) {
            fixedUrl = fixedUrl.replace("localhost", "10.0.2.2", ignoreCase = true)
        }

        if (fixedUrl.startsWith("http", ignoreCase = true)) {
            return fixedUrl
        }

        val baseUrl = BuildConfig.API_BASE_URL.trimEnd('/')
        val relativeUrl = fixedUrl.trimStart('~').trimStart('/')

        return if (!relativeUrl.contains("/")) {
            "$baseUrl/legacy/Content/img/$relativeUrl"
        } else {
            "$baseUrl/$relativeUrl"
        }
    }
}
