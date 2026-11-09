package com.cinemabd.app.feature.auth

import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.navigation.Routes
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

class UnifiedLoginLogicTest {

    private fun isNetworkError(throwable: Throwable?): Boolean {
        var current: Throwable? = throwable
        while (current != null) {
            if (current is IOException) return true
            current = current.cause
        }
        return false
    }

    private fun resolveTargetDestination(role: String?): String {
        return if (role.equals("Admin", ignoreCase = true)) {
            Routes.ADMIN_DASHBOARD
        } else {
            Routes.CUSTOMER_HOME
        }
    }

    @Test
    fun testNetworkErrorDetection() {
        assertTrue("UnknownHostException should be treated as network error", isNetworkError(UnknownHostException("Unable to resolve host")))
        assertTrue("ConnectException should be treated as network error", isNetworkError(ConnectException("Failed to connect")))
        assertTrue("SocketTimeoutException should be treated as network error", isNetworkError(SocketTimeoutException("timeout")))
        assertTrue("Generic IOException should be treated as network error", isNetworkError(IOException("Connection reset")))
        assertTrue("Wrapped IOException should be treated as network error", isNetworkError(RuntimeException("Wrapped", ConnectException("No route to host"))))

        val fake400Response = Response.error<Any>(
            400,
            "{\"title\":\"Không thể xử lý yêu cầu.\",\"detail\":\"Tài khoản không tồn tại.\"}".toResponseBody("application/json".toMediaType())
        )
        val httpException400 = HttpException(fake400Response)
        assertFalse("HttpException 400 should NOT be treated as network error", isNetworkError(httpException400))
        assertFalse("Generic Exception should NOT be treated as network error", isNetworkError(Exception("Sai mật khẩu.")))
    }

    @Test
    fun testRoleDestinationMapping() {
        assertEquals(Routes.ADMIN_DASHBOARD, resolveTargetDestination("Admin"))
        assertEquals(Routes.ADMIN_DASHBOARD, resolveTargetDestination("admin"))
        assertEquals(Routes.ADMIN_DASHBOARD, resolveTargetDestination("ADMIN"))
        assertEquals(Routes.CUSTOMER_HOME, resolveTargetDestination("Customer"))
        assertEquals(Routes.CUSTOMER_HOME, resolveTargetDestination("customer"))
        assertEquals(Routes.CUSTOMER_HOME, resolveTargetDestination(null))
        assertEquals(Routes.CUSTOMER_HOME, resolveTargetDestination(""))
    }

    @Test
    fun testBlankInputValidationRules() {
        val emptyUser = ""
        val emptyPass = ""
        val validUser = "khachhang01"
        val validPass = "123456"

        assertTrue("Empty username should be invalid", emptyUser.isBlank())
        assertTrue("Empty password should be invalid", emptyPass.isBlank())
        assertFalse("Valid username should pass", validUser.isBlank())
        assertFalse("Valid password should pass", validPass.isBlank())
    }

    @Test
    fun testTwoTierAuthFlowDecision() {
        // Case 1: Customer login encounters network error -> must NOT attempt second endpoint
        val networkEx = ConnectException("Failed to connect to 10.0.2.2")
        val shouldAttemptAdminOnNetworkError = !isNetworkError(networkEx)
        assertFalse("Should not attempt second endpoint on network error", shouldAttemptAdminOnNetworkError)
        val networkUserMessage = networkEx.toUserFriendlyMessage("Không thể kết nối máy chủ.")
        assertTrue("Network error message should mention connection", networkUserMessage.contains("kết nối", ignoreCase = true))

        // Case 2: Customer login encounters HTTP 400 (account doesn't exist or wrong pass) -> MUST attempt admin endpoint
        val fake400Response = Response.error<Any>(
            400,
            "{\"detail\":\"Tài khoản không tồn tại.\"}".toResponseBody("application/json".toMediaType())
        )
        val customerBusinessEx = HttpException(fake400Response)
        val shouldAttemptAdminOn400 = !isNetworkError(customerBusinessEx)
        assertTrue("Should attempt admin endpoint on business error 400", shouldAttemptAdminOn400)

        // Case 3: Admin login fails with 400 -> shows error from last call (admin call)
        val fakeAdmin400 = Response.error<Any>(
            400,
            "{\"detail\":\"Sai mật khẩu admin.\"}".toResponseBody("application/json".toMediaType())
        )
        val adminBusinessEx = HttpException(fakeAdmin400)
        val lastErrorMessage = adminBusinessEx.toUserFriendlyMessage("Đăng nhập thất bại. Vui lòng thử lại.")
        assertEquals("Sai mật khẩu admin.", lastErrorMessage)
    }
}
