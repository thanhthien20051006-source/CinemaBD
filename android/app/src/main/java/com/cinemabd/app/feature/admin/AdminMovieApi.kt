package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.feature.admin.model.AdminMovieUpsertRequest
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminMovieApi {
    @GET("api/admin/movies")
    suspend fun getMovies(@Query("search") search: String? = null): ApiResponse<List<MovieResponse>>

    @GET("api/admin/movies/{id}")
    suspend fun getMovie(@Path("id") movieId: String): ApiResponse<MovieResponse>

    @PUT("api/admin/movies/{id}")
    suspend fun updateMovie(
        @Path("id") movieId: String,
        @Body request: AdminMovieUpsertRequest
    ): ApiResponse<MovieResponse>
}
