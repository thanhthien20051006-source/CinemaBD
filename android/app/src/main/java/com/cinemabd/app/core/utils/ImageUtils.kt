package com.cinemabd.app.core.utils

import com.cinemabd.app.BuildConfig

fun formatImageUrl(url: String?): String? {
    if (url.isNullOrBlank()) return null

    var formattedUrl = url

    // Thay thế localhost bằng 10.0.2.2 cho emulator (nếu backend trả về full url nhưng dùng localhost)
    if (formattedUrl.contains("localhost", ignoreCase = true)) {
        formattedUrl = formattedUrl.replace("localhost", "10.0.2.2", ignoreCase = true)
    }

    if (formattedUrl.startsWith("http", ignoreCase = true)) {
        return formattedUrl
    }

    val baseUrl = BuildConfig.API_BASE_URL.trimEnd('/')
    val relativeUrl = formattedUrl.trimStart('~').trimStart('/')

    return if (!relativeUrl.contains("/")) {
        "$baseUrl/legacy/Content/img/$relativeUrl"
    } else {
        "$baseUrl/$relativeUrl"
    }
}
