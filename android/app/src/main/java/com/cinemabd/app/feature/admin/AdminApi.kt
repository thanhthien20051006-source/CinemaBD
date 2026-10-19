package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.AdminInvoiceDetail
import com.cinemabd.app.feature.admin.model.AdminRevenueStatistics
import com.cinemabd.app.feature.admin.model.CheckInRequest
import com.cinemabd.app.feature.admin.model.CheckInResultData
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface AdminApi {
    @GET("api/admin/invoices")
    suspend fun getInvoices(): ApiResponse<List<AdminInvoiceDetail>>

    @GET("api/admin/invoices/{id}")
    suspend fun getInvoice(@Path("id") invoiceId: String): ApiResponse<AdminInvoiceDetail>

    @GET("api/admin/statistics/revenue")
    suspend fun getRevenueStatistics(@Query("year") year: Int): ApiResponse<AdminRevenueStatistics>

    @POST("api/admin/invoices/check-in")
    suspend fun checkIn(@Body request: CheckInRequest): ApiResponse<CheckInResultData>
}
