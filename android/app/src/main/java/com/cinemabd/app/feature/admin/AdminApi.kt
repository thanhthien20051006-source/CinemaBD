package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.ApiResponse
import com.cinemabd.app.feature.admin.model.CheckInRequest
import com.cinemabd.app.feature.admin.model.CheckInResultData
import retrofit2.http.Body
import retrofit2.http.POST

interface AdminApi {
    @POST("api/admin/invoices/check-in")
    suspend fun checkIn(@Body request: CheckInRequest): ApiResponse<CheckInResultData>
}
