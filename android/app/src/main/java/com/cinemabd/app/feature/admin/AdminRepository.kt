package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics
import com.cinemabd.app.feature.admin.model.CheckInRequest
import com.cinemabd.app.feature.admin.model.CheckInResultData
import com.cinemabd.app.feature.admin.model.AdminShowtime
import com.cinemabd.app.feature.admin.model.AdminMovieUpsertRequest
import com.cinemabd.app.feature.admin.model.AdminShowtimeGenerateRequest
import com.cinemabd.app.feature.admin.model.AdminShowtimeGenerateResult
import com.cinemabd.app.feature.admin.model.AdminShowtimeUpsertRequest
import com.cinemabd.app.feature.admin.model.ExpirePassedShowtimesResult
import com.cinemabd.app.feature.admin.model.AdminCombo
import com.cinemabd.app.feature.admin.model.AdminComboRequest
import com.cinemabd.app.feature.admin.model.AdminVoucher
import com.cinemabd.app.feature.admin.model.AdminVoucherRequest
import com.cinemabd.app.feature.admin.model.AdminRefundRequest
import com.cinemabd.app.feature.admin.model.AdminRefundActionRequest
import com.cinemabd.app.feature.admin.model.AdminRoom
import com.cinemabd.app.core.network.model.MovieResponse
import retrofit2.HttpException
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AdminRepository @Inject constructor(
    private val adminApi: AdminApi,
    private val showtimeApi: AdminShowtimeApi,
    private val movieApi: AdminMovieApi,
    private val comboApi: AdminComboApi,
    private val voucherApi: AdminVoucherApi,
    private val refundApi: AdminRefundApi,
    private val roomApi: AdminRoomApi
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

    suspend fun getShowtimes(date: String? = null, roomId: String? = null): Result<ApiResponse<List<AdminShowtime>>> = request {
        showtimeApi.getShowtimes(date, roomId)
    }

    suspend fun createShowtime(request: AdminShowtimeUpsertRequest): Result<ApiResponse<AdminShowtime>> = request {
        showtimeApi.createShowtime(request)
    }

    suspend fun updateShowtime(showtimeId: String, request: AdminShowtimeUpsertRequest): Result<ApiResponse<AdminShowtime>> = request {
        showtimeApi.updateShowtime(showtimeId, request)
    }

    suspend fun deleteShowtime(showtimeId: String): Result<ApiResponse<Unit>> = request {
        showtimeApi.deleteShowtime(showtimeId)
    }

    suspend fun cancelShowtime(showtimeId: String): Result<ApiResponse<Unit>> = request {
        showtimeApi.cancelShowtime(showtimeId)
    }

    suspend fun generateShowtimes(request: AdminShowtimeGenerateRequest): Result<ApiResponse<AdminShowtimeGenerateResult>> = request {
        showtimeApi.generateShowtimes(request)
    }

    suspend fun expirePassedShowtimes(): Result<ApiResponse<ExpirePassedShowtimesResult>> = request {
        showtimeApi.expirePassedShowtimes()
    }

    suspend fun getMovies(search: String?): Result<ApiResponse<List<MovieResponse>>> = request {
        movieApi.getMovies(search?.takeIf(String::isNotBlank))
    }

    suspend fun getMovie(movieId: String): Result<ApiResponse<MovieResponse>> = request {
        movieApi.getMovie(movieId)
    }

    suspend fun createMovie(request: AdminMovieUpsertRequest): Result<ApiResponse<MovieResponse>> = request {
        movieApi.createMovie(request)
    }

    suspend fun updateMovie(movieId: String, request: AdminMovieUpsertRequest): Result<ApiResponse<MovieResponse>> = request {
        movieApi.updateMovie(movieId, request)
    }

    suspend fun deleteMovie(movieId: String): Result<ApiResponse<Unit>> = request {
        movieApi.deleteMovie(movieId)
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

    suspend fun getCombos(): Result<ApiResponse<List<AdminCombo>>> = request {
        comboApi.getCombos()
    }

    suspend fun createCombo(request: AdminComboRequest): Result<ApiResponse<AdminCombo>> = request {
        comboApi.createCombo(request)
    }

    suspend fun updateCombo(id: String, request: AdminComboRequest): Result<ApiResponse<AdminCombo>> = request {
        comboApi.updateCombo(id, request)
    }

    suspend fun deleteCombo(id: String): Result<ApiResponse<Unit>> = request {
        comboApi.deleteCombo(id)
    }

    suspend fun getVouchers(search: String? = null): Result<ApiResponse<List<AdminVoucher>>> = request {
        voucherApi.getVouchers(search?.takeIf(String::isNotBlank))
    }

    suspend fun createVoucher(request: AdminVoucherRequest): Result<ApiResponse<AdminVoucher>> = request {
        voucherApi.createVoucher(request)
    }

    suspend fun deleteVoucher(id: String): Result<ApiResponse<Unit>> = request {
        voucherApi.deleteVoucher(id)
    }

    suspend fun getRefunds(status: String? = null): Result<ApiResponse<List<AdminRefundRequest>>> = request {
        refundApi.getRefunds(status?.takeIf(String::isNotBlank))
    }

    suspend fun approveRefund(id: Int, note: String? = null): Result<ApiResponse<Unit>> = request {
        refundApi.approveRefund(id, note?.let { AdminRefundActionRequest(it) })
    }

    suspend fun rejectRefund(id: Int, note: String? = null): Result<ApiResponse<Unit>> = request {
        refundApi.rejectRefund(id, note?.let { AdminRefundActionRequest(it) })
    }

    suspend fun getRooms(cinemaId: String? = null): Result<ApiResponse<List<AdminRoom>>> = request {
        roomApi.getRooms(cinemaId)
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
