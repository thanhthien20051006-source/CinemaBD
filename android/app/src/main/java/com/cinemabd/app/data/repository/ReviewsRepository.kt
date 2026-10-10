package com.cinemabd.app.data.repository

import com.cinemabd.app.core.network.ReviewsApi
import com.cinemabd.app.core.network.model.ReviewResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ReviewsRepository @Inject constructor(
    private val reviewsApi: ReviewsApi
) {
    suspend fun getReviewsByMovie(movieId: String): Result<List<ReviewResponse>> = withContext(Dispatchers.IO) {
        try {
            val response = reviewsApi.getReviewsByMovie(movieId)
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message ?: "Failed to fetch reviews"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
