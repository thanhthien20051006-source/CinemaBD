package com.cinemabd.app.core.network

import kotlinx.serialization.ExperimentalSerializationApi
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonNames

@OptIn(ExperimentalSerializationApi::class)
@Serializable
data class ApiResponse<T>(
    @JsonNames("Success", "success")
    val success: Boolean = false,
    @JsonNames("Message", "message")
    val message: String = "",
    @JsonNames("Data", "data")
    val data: T? = null
)
