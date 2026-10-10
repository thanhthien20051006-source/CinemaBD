package com.cinemabd.app.core.network

import com.cinemabd.app.core.network.model.CustomerHistoryResponse
import com.cinemabd.app.core.network.model.CustomerProfileResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.model.LoyaltyPointResponse
import com.cinemabd.app.core.network.model.UpdateCustomerProfileRequest
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PUT
import retrofit2.http.Path

interface AccountApi {
    @GET("api/account/profile")
    suspend fun getProfile(): ApiResponse<CustomerProfileResponse>

    @PUT("api/account/profile")
    suspend fun updateProfile(@Body request: UpdateCustomerProfileRequest): ApiResponse<CustomerProfileResponse>

    @GET("api/account/loyalty")
    suspend fun getLoyalty(): ApiResponse<LoyaltyPointResponse>

    @GET("api/account/history")
    suspend fun getHistory(): ApiResponse<List<CustomerHistoryResponse>>

    @GET("api/bookings/invoice/{txnRef}")
    suspend fun getInvoiceDetail(@Path("txnRef") txnRef: String): ApiResponse<InvoiceResponse>
}
