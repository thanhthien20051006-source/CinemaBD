package com.cinemabd.app.di

import android.content.Context
import androidx.room.Room
import com.cinemabd.app.core.database.CinemaDatabase
import com.cinemabd.app.core.database.dao.TicketDao
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object DatabaseModule {

    @Provides
    @Singleton
    fun provideCinemaDatabase(
        @ApplicationContext context: Context
    ): CinemaDatabase {
        return Room.databaseBuilder(
            context,
            CinemaDatabase::class.java,
            "cinema_bd.db"
        ).fallbackToDestructiveMigration().build()
    }

    @Provides
    @Singleton
    fun provideTicketDao(database: CinemaDatabase): TicketDao {
        return database.ticketDao()
    }
}
