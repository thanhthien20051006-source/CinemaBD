package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.MovieResponse
import retrofit2.http.GET
import retrofit2.http.Path

interface MoviesApi {
    @GET("api/movies")
    suspend fun getMovies(): ApiResponse<List<MovieResponse>>

    @GET("api/movies/{id}")
    suspend fun getMovieById(@Path("id") id: String): ApiResponse<MovieResponse>
}
