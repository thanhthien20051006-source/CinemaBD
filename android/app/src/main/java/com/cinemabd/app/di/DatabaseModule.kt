package com.cinemabd.app.di

import android.content.Context
import androidx.room.Room
import com.cinemabd.app.core.database.CinemaAppDatabase
import com.cinemabd.app.core.database.MIGRATION_1_2
import com.cinemabd.app.core.database.MIGRATION_2_3
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
    ): CinemaAppDatabase {
        return Room.databaseBuilder(
            context,
            CinemaAppDatabase::class.java,
            "cinema_bd.db"
        )
            .addMigrations(MIGRATION_1_2, MIGRATION_2_3)
            .fallbackToDestructiveMigration()
            .build()
    }

    @Provides
    @Singleton
    fun provideTicketDao(database: CinemaAppDatabase): TicketDao {
        return database.ticketDao()
    }
}
