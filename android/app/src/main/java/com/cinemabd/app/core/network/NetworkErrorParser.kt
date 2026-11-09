package com.cinemabd.app.core.network

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import retrofit2.HttpException
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

private val errorJson = Json { ignoreUnknownKeys = true }

fun Throwable?.toUserFriendlyMessage(defaultMessage: String = "Đã xảy ra lỗi. Vui lòng thử lại."): String {
    if (this == null) return defaultMessage

    return when (this) {
        is UnknownHostException, is ConnectException -> "Không thể kết nối máy chủ. Vui lòng kiểm tra lại kết nối mạng."
        is SocketTimeoutException -> "Kết nối quá thời gian chờ. Vui lòng thử lại sau."
        is IOException -> "Lỗi kết nối mạng. Vui lòng thử lại."
        is HttpException -> {
            if (code() == 429) return "Bạn thao tác quá nhanh. Vui lòng chờ một chút rồi thử lại."

            val bodyMessage = response()?.errorBody()?.use { body ->
                runCatching { parseServerMessage(body.string()) }.getOrNull()
            }
            if (!bodyMessage.isNullOrBlank()) return bodyMessage

            when (code()) {
                400 -> "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại."
                401 -> "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                403 -> "Bạn không có quyền thực hiện thao tác này."
                404 -> "Không tìm thấy dữ liệu yêu cầu."
                409 -> "Khung giờ này đã bị trùng lịch với suất chiếu khác. Vui lòng chọn giờ hoặc phòng khác."
                in 500..599 -> "Hệ thống máy chủ đang bận. Vui lòng thử lại sau."
                else -> message().ifBlank { defaultMessage }
            }
        }
        else -> {
            val msg = message?.trim()
            if (!msg.isNullOrBlank() &&
                !msg.startsWith("java.") &&
                !msg.startsWith("kotlin.") &&
                !msg.contains("Exception") &&
                !msg.contains("failed to connect", ignoreCase = true)
            ) msg else defaultMessage
        }
    }
}

private fun parseServerMessage(body: String): String? {
    if (body.isBlank()) return null
    val root = errorJson.parseToJsonElement(body).jsonObject

    return root.firstString("message", "Message", "detail", "Detail", "title", "Title")
        ?: root["errors"]?.let(::firstValidationError)
}

private fun JsonElement.firstString(vararg keys: String): String? {
    val obj = runCatching { jsonObject }.getOrNull() ?: return null
    return keys.firstNotNullOfOrNull { key ->
        obj[key]?.let { value ->
            runCatching { value.jsonPrimitive.content.trim() }.getOrNull()?.takeIf(String::isNotBlank)
        }
    }
}

private fun firstValidationError(errors: JsonElement): String? {
    val obj = runCatching { errors.jsonObject }.getOrNull() ?: return null
    return obj.values.firstNotNullOfOrNull { value ->
        runCatching { value.toString().trim('[', ']', '"') }.getOrNull()?.takeIf(String::isNotBlank)
    }
}
