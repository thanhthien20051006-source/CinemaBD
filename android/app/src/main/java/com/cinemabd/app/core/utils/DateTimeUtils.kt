package com.cinemabd.app.core.utils

import java.text.NumberFormat
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.Locale

object DateTimeUtils {

    fun isUpcoming(showDate: String?, startTime: String?): Boolean {
        if (showDate.isNullOrBlank()) return false
        return try {
            val date = parseLocalDate(showDate) ?: return false
            val time = parseLocalTime(startTime) ?: LocalTime.MIN
            val showDateTime = LocalDateTime.of(date, time)
            showDateTime.isAfter(LocalDateTime.now())
        } catch (e: Exception) {
            false
        }
    }

    fun formatShowDateTime(showDate: String?, startTime: String?): String {
        val date = parseLocalDate(showDate)
        val time = parseLocalTime(startTime)

        return when {
            date != null && time != null -> {
                val timeStr = time.format(DateTimeFormatter.ofPattern("HH:mm"))
                val dateStr = date.format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
                "$timeStr - $dateStr"
            }
            date != null -> {
                date.format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
            }
            else -> "—"
        }
    }

    fun formatCurrency(amount: Double): String {
        val formatter = NumberFormat.getNumberInstance(Locale("vi", "VN"))
        return "${formatter.format(amount.toLong())} đ"
    }

    private fun parseLocalDate(dateStr: String?): LocalDate? {
        if (dateStr.isNullOrBlank()) return null
        return try {
            val clean = dateStr.substringBefore('T').trim()
            LocalDate.parse(clean, DateTimeFormatter.ISO_LOCAL_DATE)
        } catch (e: Exception) {
            null
        }
    }

    private fun parseLocalTime(timeStr: String?): LocalTime? {
        if (timeStr.isNullOrBlank()) return null
        return try {
            val clean = timeStr.substringBefore('.').trim()
            when (clean.count { it == ':' }) {
                2 -> LocalTime.parse(clean, DateTimeFormatter.ofPattern("HH:mm:ss"))
                1 -> LocalTime.parse(clean, DateTimeFormatter.ofPattern("HH:mm"))
                else -> null
            }
        } catch (e: Exception) {
            null
        }
    }
}
