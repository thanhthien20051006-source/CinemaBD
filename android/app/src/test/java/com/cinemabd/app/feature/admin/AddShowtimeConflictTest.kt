package com.cinemabd.app.feature.admin

import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.feature.admin.model.AdminRoom
import com.cinemabd.app.feature.admin.model.AdminShowtime
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.format.DateTimeFormatter

class AddShowtimeConflictTest {

    private val testMovie = MovieResponse(
        id = "M01",
        title = "Mai",
        durationMinutes = 131
    )

    private val testRoom = AdminRoom(
        id = "R01",
        name = "Phòng 1",
        seatCount = 100
    )

    @Test
    fun testExpectedEndTimeCalculation() {
        val startTime = LocalTime.of(14, 0)
        val duration = testMovie.durationMinutes
        val buffer = 15L
        val endTime = startTime.plusMinutes(duration + buffer)

        // 14:00 + 131 + 15 = 14:00 + 146 min = 16:26
        assertEquals(LocalTime.of(16, 26), endTime)
    }

    @Test
    fun testOverlapCollisionDetected() {
        val date = LocalDate.of(2026, 10, 25)
        // Existing showtime: 14:00 -> (14:00 + 131 + 15) = 16:26
        val existingStart = LocalTime.of(14, 0)
        val existingEnd = existingStart.plusMinutes(131 + 15L)

        val existingStartDT = LocalDateTime.of(date, existingStart)
        val existingEndDT = LocalDateTime.of(date, existingEnd)

        // New showtime: 15:30 -> 17:56 (overlaps with 14:00-16:26)
        val newStart = LocalTime.of(15, 30)
        val newEnd = newStart.plusMinutes(131 + 15L)

        val newStartDT = LocalDateTime.of(date, newStart)
        val newEndDT = LocalDateTime.of(date, newEnd)

        val maxStart = if (newStartDT.isAfter(existingStartDT)) newStartDT else existingStartDT
        val minEnd = if (newEndDT.isBefore(existingEndDT)) newEndDT else existingEndDT

        assertTrue("Should detect overlap when max(Start_N, Start_E) < min(End_N, End_E)", maxStart.isBefore(minEnd))
    }

    @Test
    fun testAdjacentNoOverlap() {
        val date = LocalDate.of(2026, 10, 25)
        // Existing: 14:00 -> 16:26
        val existingStart = LocalTime.of(14, 0)
        val existingEnd = existingStart.plusMinutes(131 + 15L)

        val existingStartDT = LocalDateTime.of(date, existingStart)
        val existingEndDT = LocalDateTime.of(date, existingEnd)

        // New: 16:26 -> 18:52 (starts exactly when previous ends)
        val newStart = LocalTime.of(16, 26)
        val newEnd = newStart.plusMinutes(131 + 15L)

        val newStartDT = LocalDateTime.of(date, newStart)
        val newEndDT = LocalDateTime.of(date, newEnd)

        val maxStart = if (newStartDT.isAfter(existingStartDT)) newStartDT else existingStartDT
        val minEnd = if (newEndDT.isBefore(existingEndDT)) newEndDT else existingEndDT

        assertFalse("Adjacent showtime should not overlap", maxStart.isBefore(minEnd))
    }

    @Test
    fun testPriceValidationRule() {
        val stateUnderMin = AddShowtimeUiState(ticketPrice = "39999")
        assertFalse("Price < 40,000 must be invalid", stateUnderMin.isPriceValid)

        val stateMin = AddShowtimeUiState(ticketPrice = "40000")
        assertTrue("Price == 40,000 must be valid", stateMin.isPriceValid)

        val stateValid = AddShowtimeUiState(ticketPrice = "75000")
        assertTrue("Price > 40,000 must be valid", stateValid.isPriceValid)
    }

    @Test
    fun testPastTimeValidationRule() {
        val yesterday = LocalDate.now().minusDays(1)
        val pastState = AddShowtimeUiState(
            selectedDate = yesterday,
            selectedTime = LocalTime.of(10, 0)
        )
        assertTrue("Yesterday's date must be in the past", pastState.isTimeInPast)

        val future = LocalDate.now().plusDays(2)
        val futureState = AddShowtimeUiState(
            selectedDate = future,
            selectedTime = LocalTime.of(10, 0)
        )
        assertFalse("Future date must not be in the past", futureState.isTimeInPast)
    }
}
