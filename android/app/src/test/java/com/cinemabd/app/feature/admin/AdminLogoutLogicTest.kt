package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.session.SessionManager
import com.cinemabd.app.core.session.UserRole
import com.cinemabd.app.core.session.UserSession
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AdminLogoutLogicTest {

    @Test
    fun testSessionManagerClearSessionInvokesCallback() = runBlocking {
        var isCleared = false
        val sessionManager = SessionManager(
            onClearSession = {
                isCleared = true
            }
        )

        assertFalse("Session should initially not be cleared", isCleared)
        sessionManager.clearSession()
        assertTrue("SessionManager.clearSession should call onClearSession", isCleared)
    }

    @Test
    fun testAdminDisplayNameResolution() {
        val defaultRoleString = "Quản trị viên"

        val nullName: String? = null
        val emptyName = "   "
        val customName = "Nguyễn Văn Admin"

        val resolvedNull = nullName?.takeIf { it.isNotBlank() } ?: defaultRoleString
        val resolvedEmpty = emptyName.takeIf { it.isNotBlank() } ?: defaultRoleString
        val resolvedCustom = customName.takeIf { it.isNotBlank() } ?: defaultRoleString

        assertEquals("Quản trị viên", resolvedNull)
        assertEquals("Quản trị viên", resolvedEmpty)
        assertEquals("Nguyễn Văn Admin", resolvedCustom)
    }

    @Test
    fun testAdminUserRoleResolution() {
        assertEquals(UserRole.Admin, UserRole.from("Admin"))
        assertEquals(UserRole.Admin, UserRole.from("admin"))
        assertEquals(UserRole.Customer, UserRole.from("Customer"))
        assertEquals(null, UserRole.from("Unknown"))
    }

    @Test
    fun testSessionManagerResetSession() = runBlocking {
        val sessionManager = SessionManager(onClearSession = {})
        assertFalse(sessionManager.isSessionExpired())
        sessionManager.emitSessionExpired()
        assertTrue(sessionManager.isSessionExpired())
        sessionManager.resetSession()
        assertFalse(sessionManager.isSessionExpired())
    }

    @Test
    fun testSessionManagerClearSessionResetsExpiredState() = runBlocking {
        val sessionManager = SessionManager(onClearSession = {})
        sessionManager.emitSessionExpired()
        assertTrue(sessionManager.isSessionExpired())
        sessionManager.clearSession()
        assertFalse(sessionManager.isSessionExpired())
    }

    @Test
    fun testRoutesAuthAndLoginEquivalence() {
        assertEquals("auth", com.cinemabd.app.navigation.Routes.AUTH)
        assertEquals(com.cinemabd.app.navigation.Routes.AUTH, com.cinemabd.app.navigation.Routes.LOGIN)
    }
}
