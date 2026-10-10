package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.ReviewResponse
import retrofit2.http.GET
import retrofit2.http.Path

interface ReviewsApi {
    @GET("api/reviews/movie/{movieId}")
    suspend fun getReviewsByMovie(@Path("movieId") movieId: String): ApiResponse<List<ReviewResponse>>
}
