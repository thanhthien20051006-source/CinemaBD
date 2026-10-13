package com.cinemabd.app.core.utils

import android.util.Base64
import org.json.JSONObject

object JwtUtils {
    fun parseRole(jwtToken: String): String? {
        return try {
            val parts = jwtToken.split(".")
            if (parts.size >= 2) {
                val decodedBytes = Base64.decode(
                    parts[1],
                    Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP
                )
                val json = JSONObject(String(decodedBytes, Charsets.UTF_8))
                val roleUri = json.optString("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ifEmpty { null }
                val role = json.optString("role").ifEmpty { null }
                val accountType = json.optString("account_type").ifEmpty { null }
                when {
                    !roleUri.isNullOrEmpty() -> roleUri
                    !role.isNullOrEmpty() -> role
                    accountType.equals("admin", ignoreCase = true) -> "Admin"
                    else -> null
                }
            } else null
        } catch (_: Exception) {
            null
        }
    }

    fun isRoleAdmin(jwtToken: String, responseRole: String? = null): Boolean {
        if (responseRole.equals("Admin", ignoreCase = true)) return true
        val role = parseRole(jwtToken)
        return role.equals("Admin", ignoreCase = true)
    }
}
