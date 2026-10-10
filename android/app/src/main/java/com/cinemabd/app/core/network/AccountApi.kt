package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.CustomerHistoryResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import retrofit2.http.GET
import retrofit2.http.Path

interface AccountApi {
    @GET("api/account/history")
    suspend fun getHistory(): ApiResponse<List<CustomerHistoryResponse>>

    @GET("api/bookings/invoice/{txnRef}")
    suspend fun getInvoiceDetail(@Path("txnRef") txnRef: String): ApiResponse<InvoiceResponse>
}
