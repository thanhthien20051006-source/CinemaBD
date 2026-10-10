package com.cinemabd.app.core.utils

import com.cinemabd.app.BuildConfig

fun formatImageUrl(url: String?): String? {
    if (url.isNullOrBlank()) return null
    
    var formattedUrl = url
    
    // Nếu URL là đường dẫn tương đối (ví dụ /img/Posters/abc.jpg hoặc ~/img/...)
    if (formattedUrl.startsWith("/") || formattedUrl.startsWith("~")) {
        val baseUrl = BuildConfig.API_BASE_URL.trimEnd('/')
        // Xóa dấu ~ nếu có
        if (formattedUrl.startsWith("~")) {
            formattedUrl = formattedUrl.substring(1)
        }
        // Đảm bảo có dấu / ở đầu
        if (!formattedUrl.startsWith("/")) {
            formattedUrl = "/$formattedUrl"
        }
        formattedUrl = baseUrl + formattedUrl
    }
    
    // Thay thế localhost bằng 10.0.2.2 cho emulator (nếu backend trả về full url nhưng dùng localhost)
    if (formattedUrl.contains("localhost")) {
        formattedUrl = formattedUrl.replace("localhost", "10.0.2.2")
    }
    
    return formattedUrl
}
