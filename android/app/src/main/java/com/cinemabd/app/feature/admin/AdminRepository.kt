package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics
import com.cinemabd.app.feature.admin.model.CheckInRequest
import com.cinemabd.app.feature.admin.model.CheckInResultData
import com.cinemabd.app.feature.admin.model.AdminShowtime
import com.cinemabd.app.feature.admin.model.AdminMovieUpsertRequest
import com.cinemabd.app.core.network.model.MovieResponse
import retrofit2.HttpException
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AdminRepository @Inject constructor(
    private val adminApi: AdminApi,
    private val showtimeApi: AdminShowtimeApi,
    private val movieApi: AdminMovieApi
) {
    suspend fun getInvoices(): Result<ApiResponse<List<AdminInvoiceDetail>>> = request {
        adminApi.getInvoices()
    }

    suspend fun getInvoice(invoiceId: String): Result<ApiResponse<AdminInvoiceDetail>> = request {
        adminApi.getInvoice(invoiceId)
    }

    suspend fun getRevenueStatistics(year: Int): Result<ApiResponse<AdminRevenueStatistics>> = request {
        adminApi.getRevenueStatistics(year)
    }

    suspend fun checkIn(qrText: String): Result<ApiResponse<CheckInResultData>> {
        return try {
            val response = adminApi.checkIn(CheckInRequest(qrText.trim()))
            Result.success(response)
        } catch (e: HttpException) {
            if (e.code() == 403) {
                Result.failure(SecurityException("Tài khoản không có quyền soát vé"))
            } else {
                Result.failure(e)
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getShowtimes(date: String): Result<ApiResponse<List<AdminShowtime>>> = request {
        showtimeApi.getShowtimes(date)
    }

    suspend fun cancelShowtime(showtimeId: String): Result<ApiResponse<Unit>> = request {
        showtimeApi.cancelShowtime(showtimeId)
    }

    suspend fun getMovies(search: String?): Result<ApiResponse<List<MovieResponse>>> = request {
        movieApi.getMovies(search?.takeIf(String::isNotBlank))
    }

    suspend fun getMovie(movieId: String): Result<ApiResponse<MovieResponse>> = request {
        movieApi.getMovie(movieId)
    }

    suspend fun updateMovieStatus(movieId: String, status: String): Result<ApiResponse<MovieResponse>> = request {
        val detail = movieApi.getMovie(movieId)
        val movie = detail.data
        if (!detail.success || movie == null) {
            throw IllegalStateException(detail.message.ifBlank { "Không thể tải đầy đủ thông tin phim để cập nhật." })
        }

        movieApi.updateMovie(
            movie.id,
            AdminMovieUpsertRequest(
                title = movie.title,
                genre = movie.genre,
                durationMinutes = movie.durationMinutes,
                director = movie.director,
                cast = movie.cast,
                country = movie.country,
                ageRestriction = movie.ageRestriction,
                description = movie.description,
                posterUrl = movie.posterUrl,
                trailerUrl = movie.trailerUrl,
                releaseDate = movie.releaseDate?.substringBefore('T'),
                endDate = movie.endDate?.substringBefore('T'),
                status = status
            )
        )
    }

    private suspend fun <T> request(block: suspend () -> T): Result<T> = try {
        Result.success(block())
    } catch (e: HttpException) {
        if (e.code() == 403) {
            Result.failure(SecurityException("Tài khoản không có quyền truy cập quản trị"))
        } else {
            Result.failure(e)
        }
    } catch (e: Exception) {
        Result.failure(e)
    }
}
