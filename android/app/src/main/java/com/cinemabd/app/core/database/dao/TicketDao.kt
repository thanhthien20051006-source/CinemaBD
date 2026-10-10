package com.cinemabd.app.core.database.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import com.cinemabd.app.core.database.entity.TicketEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface TicketDao {
    @Query("SELECT * FROM tickets ORDER BY paymentDate DESC")
    fun getAllTicketsFlow(): Flow<List<TicketEntity>>

    @Query("SELECT * FROM tickets ORDER BY paymentDate DESC")
    suspend fun getAllTickets(): List<TicketEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertTickets(tickets: List<TicketEntity>)

    @Query("SELECT * FROM tickets WHERE invoiceId = :invoiceId")
    suspend fun getTicketById(invoiceId: String): TicketEntity?

    @Query("DELETE FROM tickets")
    suspend fun clearTickets()
}
