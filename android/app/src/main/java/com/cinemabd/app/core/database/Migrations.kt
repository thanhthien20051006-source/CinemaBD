package com.cinemabd.app.core.database

import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase

val MIGRATION_1_2 = object : Migration(1, 2) {
    override fun migrate(database: SupportSQLiteDatabase) {
        database.execSQL(
            """CREATE TABLE IF NOT EXISTS tickets_v2 (
                userId TEXT NOT NULL,
                invoiceId TEXT NOT NULL,
                movieTitle TEXT,
                showDate TEXT,
                startTime TEXT,
                roomName TEXT,
                seats TEXT NOT NULL,
                status TEXT,
                totalAmount REAL NOT NULL,
                paymentDate TEXT,
                ticketCount INTEGER NOT NULL,
                checkedInCount INTEGER NOT NULL,
                PRIMARY KEY(userId, invoiceId)
            )""".trimIndent()
        )
        database.execSQL(
            """INSERT INTO tickets_v2 (
                userId, invoiceId, movieTitle, showDate, startTime, roomName, seats,
                status, totalAmount, paymentDate, ticketCount, checkedInCount
            ) SELECT '', invoiceId, movieTitle, showDate, startTime, roomName, seats,
                status, totalAmount, paymentDate, ticketCount, checkedInCount FROM tickets""".trimIndent()
        )
        database.execSQL("DROP TABLE tickets")
        database.execSQL("ALTER TABLE tickets_v2 RENAME TO tickets")
        database.execSQL(
            """CREATE TABLE IF NOT EXISTS ticket_details (
                ticketId TEXT NOT NULL,
                userId TEXT NOT NULL,
                invoiceId TEXT NOT NULL,
                movieTitle TEXT,
                roomName TEXT,
                showDate TEXT,
                startTime TEXT,
                seatId TEXT NOT NULL,
                price REAL NOT NULL,
                status TEXT,
                isCheckedIn INTEGER NOT NULL,
                checkedInAt TEXT,
                qrPayload TEXT NOT NULL,
                qrSignature TEXT NOT NULL,
                qrIssuedAtUtc TEXT,
                PRIMARY KEY(userId, ticketId)
            )""".trimIndent()
        )
        database.execSQL("CREATE INDEX IF NOT EXISTS index_ticket_details_userId_invoiceId ON ticket_details(userId, invoiceId)")
    }
}

val MIGRATION_2_3 = object : Migration(2, 3) {
    override fun migrate(database: SupportSQLiteDatabase) {
        database.execSQL("ALTER TABLE tickets ADD COLUMN qrPayload TEXT")
    }
}

