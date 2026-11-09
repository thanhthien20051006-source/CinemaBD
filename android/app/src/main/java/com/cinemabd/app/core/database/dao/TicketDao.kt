package com.cinemabd.app.core.database.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import com.cinemabd.app.core.database.entity.TicketDetailEntity
import com.cinemabd.app.core.database.entity.TicketEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface TicketDao {
    @Query("SELECT * FROM tickets WHERE userId = :userId ORDER BY paymentDate DESC")
    fun getAllTickets(userId: String): Flow<List<TicketEntity>>

    @Query("SELECT * FROM tickets WHERE userId = :userId ORDER BY paymentDate DESC")
    suspend fun getAllTicketsOnce(userId: String): List<TicketEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertTickets(tickets: List<TicketEntity>)

    @Query("SELECT * FROM tickets WHERE userId = :userId AND invoiceId = :invoiceId LIMIT 1")
    fun getTicketById(userId: String, invoiceId: String): Flow<TicketEntity?>

    @Query("SELECT * FROM tickets WHERE userId = :userId AND invoiceId = :invoiceId LIMIT 1")
    suspend fun getTicketByIdOnce(userId: String, invoiceId: String): TicketEntity?

    @Query("DELETE FROM tickets WHERE userId = :userId")
    suspend fun clearTickets(userId: String)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertTicketDetails(details: List<TicketDetailEntity>)

    @Query("SELECT * FROM ticket_details WHERE userId = :userId AND ticketId = :ticketId LIMIT 1")
    fun getTicketDetailById(userId: String, ticketId: String): Flow<TicketDetailEntity?>

    @Query("SELECT * FROM ticket_details WHERE userId = :userId AND invoiceId = :invoiceId ORDER BY ticketId")
    suspend fun getTicketDetails(userId: String, invoiceId: String): List<TicketDetailEntity>

    @Query("SELECT * FROM ticket_details WHERE userId = :userId AND invoiceId = :invoiceId ORDER BY ticketId")
    fun getTicketDetailsFlow(userId: String, invoiceId: String): Flow<List<TicketDetailEntity>>

    @Query("SELECT * FROM ticket_details WHERE userId = :userId ORDER BY invoiceId, ticketId")
    suspend fun getTicketDetailsForUser(userId: String): List<TicketDetailEntity>

    @Query("DELETE FROM ticket_details WHERE userId = :userId")
    suspend fun clearTicketDetails(userId: String)
}
