package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.PaymentCallbackResponse
import retrofit2.http.GET
import retrofit2.http.Query
import retrofit2.http.QueryMap

interface PaymentsApi {
    @GET("api/payments/demo-confirm")
    suspend fun demoConfirm(
        @Query("txnRef") txnRef: String,
        @Query("gateway") gateway: String = "MOMO"
    ): ApiResponse<PaymentCallbackResponse>

    @GET("api/payments/vnpay-return")
    suspend fun vnPayReturn(
        @QueryMap queryMap: Map<String, String>
    ): ApiResponse<PaymentCallbackResponse>
}
