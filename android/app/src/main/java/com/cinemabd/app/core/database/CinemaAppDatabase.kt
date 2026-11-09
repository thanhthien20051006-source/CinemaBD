package com.cinemabd.app.core.database

import androidx.room.Database
import androidx.room.RoomDatabase
import com.cinemabd.app.core.database.dao.TicketDao
import com.cinemabd.app.core.database.entity.TicketDetailEntity
import com.cinemabd.app.core.database.entity.TicketEntity

@Database(entities = [TicketEntity::class, TicketDetailEntity::class], version = 3, exportSchema = false)
abstract class CinemaAppDatabase : RoomDatabase() {
    abstract fun ticketDao(): TicketDao
}
