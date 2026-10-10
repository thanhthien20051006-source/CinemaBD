package com.cinemabd.app.data.repository

import com.cinemabd.app.BuildConfig
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
                val mappedData = response.data.map { movie ->
                    movie.copy(
                        posterUrl = fixImageUrl(movie.posterUrl)
                    )
                }
                Result.success(mappedData)
            } else {
                Result.failure(Exception(response.message ?: "Failed to fetch movies"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getMovieById(id: String): Result<MovieResponse> = withContext(Dispatchers.IO) {
        try {
            val response = moviesApi.getMovieById(id)
            if (response.success && response.data != null) {
                val movie = response.data.copy(
                    posterUrl = fixImageUrl(response.data.posterUrl)
                )
                Result.success(movie)
            } else {
                Result.failure(Exception(response.message ?: "Failed to fetch movie details"))
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
        val relativeUrl = fixedUrl.trimStart('/')

        return if (!relativeUrl.contains("/")) {
            "$baseUrl/legacy/Content/img/Posters/$relativeUrl"
        } else {
            "$baseUrl/$relativeUrl"
        }
    }
}
