package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminVoucher
import com.cinemabd.app.feature.admin.model.AdminVoucherRequest
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminVoucherApi {
    @GET("api/admin/vouchers")
    suspend fun getVouchers(@Query("search") search: String? = null): ApiResponse<List<AdminVoucher>>

    @GET("api/admin/vouchers/{id}")
    suspend fun getVoucher(@Path("id") id: String): ApiResponse<AdminVoucher>

    @POST("api/admin/vouchers")
    suspend fun createVoucher(@Body request: AdminVoucherRequest): ApiResponse<AdminVoucher>

    @PUT("api/admin/vouchers/{id}")
    suspend fun updateVoucher(
        @Path("id") id: String,
        @Body request: AdminVoucherRequest
    ): ApiResponse<AdminVoucher>

    @DELETE("api/admin/vouchers/{id}")
    suspend fun deleteVoucher(@Path("id") id: String): ApiResponse<Unit>
}
