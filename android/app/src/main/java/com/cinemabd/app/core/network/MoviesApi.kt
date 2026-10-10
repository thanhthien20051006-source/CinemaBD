package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.MovieResponse
import retrofit2.http.GET

interface MoviesApi {
    @GET("api/movies")
    suspend fun getMovies(): ApiResponse<List<MovieResponse>>
}
