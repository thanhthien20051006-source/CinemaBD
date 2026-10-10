package com.cinemabd.app.data.repository

import com.cinemabd.app.core.network.MoviesApi
import com.cinemabd.app.core.network.model.MovieResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class MoviesRepository @Inject constructor(
    private val moviesApi: MoviesApi
) {
    suspend fun getMovies(): Result<List<MovieResponse>> = withContext(Dispatchers.IO) {
        try {
            val response = moviesApi.getMovies()
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message ?: "Failed to fetch movies"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
