package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminRefundActionRequest
import com.cinemabd.app.feature.admin.model.AdminRefundRequest
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminRefundApi {
    @GET("api/admin/refunds")
    suspend fun getRefunds(@Query("status") status: String? = null): ApiResponse<List<AdminRefundRequest>>

    @POST("api/admin/refunds/{id}/approve")
    suspend fun approveRefund(
        @Path("id") id: Int,
        @Body request: AdminRefundActionRequest? = null
    ): ApiResponse<Unit>

    @POST("api/admin/refunds/{id}/reject")
    suspend fun rejectRefund(
        @Path("id") id: Int,
        @Body request: AdminRefundActionRequest? = null
    ): ApiResponse<Unit>
}
