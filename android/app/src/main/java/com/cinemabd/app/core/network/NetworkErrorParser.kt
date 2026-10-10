package com.cinemabd.app.core.network

import retrofit2.HttpException
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

fun Throwable?.toUserFriendlyMessage(defaultMessage: String = "Đã xảy ra lỗi. Vui lòng thử lại."): String {
    if (this == null) return defaultMessage

    return when (this) {
        is UnknownHostException, is ConnectException -> {
            "Không thể kết nối máy chủ. Vui lòng kiểm tra lại kết nối mạng."
        }
        is SocketTimeoutException -> {
            "Kết nối quá thời gian chờ. Vui lòng thử lại sau."
        }
        is IOException -> {
            "Lỗi kết nối mạng. Vui lòng thử lại."
        }
        is HttpException -> {
            try {
                val errorBody = response()?.errorBody()?.string()
                if (!errorBody.isNullOrBlank()) {
                    val regex = Regex("\"message\"\\s*:\\s*\"([^\"]+)\"")
                    val match = regex.find(errorBody)
                    if (match != null) {
                        val serverMsg = match.groupValues[1]
                        if (serverMsg.isNotBlank()) return serverMsg
                    }
                }
            } catch (_: Exception) {
                // Ignore parse error and fallback to status code
            }

            when (code()) {
                400 -> "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại."
                401 -> "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                403 -> "Bạn không có quyền thực hiện thao tác này."
                404 -> "Không tìm thấy dữ liệu yêu cầu."
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
            ) {
                msg
            } else {
                defaultMessage
            }
        }
    }
}
