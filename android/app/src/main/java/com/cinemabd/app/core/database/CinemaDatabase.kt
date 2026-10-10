package com.cinemabd.app.core.database

import androidx.room.Database
import androidx.room.RoomDatabase
import com.cinemabd.app.core.database.dao.TicketDao
import com.cinemabd.app.core.database.entity.TicketEntity

@Database(entities = [TicketEntity::class], version = 1, exportSchema = false)
abstract class CinemaDatabase : RoomDatabase() {
    abstract fun ticketDao(): TicketDao
}
