package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.core.network.model.ShowtimeResponse
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface MoviesApi {
    @GET("api/movies")
    suspend fun getMovies(): ApiResponse<List<MovieResponse>>

    @GET("api/movies/{id}")
    suspend fun getMovieById(@Path("id") id: String): ApiResponse<MovieResponse>

    @GET("api/movies/{id}/showtimes")
    suspend fun getShowtimes(
        @Path("id") id: String,
        @Query("date") date: String? = null
    ): ApiResponse<List<ShowtimeResponse>>
}
