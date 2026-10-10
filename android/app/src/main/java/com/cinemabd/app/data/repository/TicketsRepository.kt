package com.cinemabd.app.data.repository

import com.cinemabd.app.core.database.dao.TicketDao
import com.cinemabd.app.core.database.entity.TicketEntity
import com.cinemabd.app.core.network.AccountApi
import com.cinemabd.app.core.network.model.CustomerHistoryResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

data class TicketsResult(
    val tickets: List<CustomerHistoryResponse>,
    val roomMap: Map<String, String?> = emptyMap(),
    val isOffline: Boolean = false
)

@Singleton
class TicketsRepository @Inject constructor(
    private val accountApi: AccountApi,
    private val ticketDao: TicketDao
) {

    suspend fun getTickets(): Result<TicketsResult> = withContext(Dispatchers.IO) {
        try {
            val response = accountApi.getHistory()
            if (response.success && response.data != null) {
                val remoteTickets = response.data
                val existingEntities = ticketDao.getAllTickets().associateBy { it.invoiceId }

                val newEntities = remoteTickets.map { dto ->
                    val existing = existingEntities[dto.invoiceId]
                    TicketEntity(
                        invoiceId = dto.invoiceId,
                        movieTitle = dto.movieTitle,
                        showDate = dto.showDate,
                        startTime = dto.startTime,
                        roomName = existing?.roomName,
                        seats = dto.seatIds.joinToString(", "),
                        status = dto.status,
                        totalAmount = dto.totalAmount,
                        paymentDate = dto.paymentDate,
                        ticketCount = dto.ticketCount,
                        checkedInCount = dto.checkedInCount
                    )
                }

                ticketDao.clearTickets()
                ticketDao.insertTickets(newEntities)

                val roomMap = newEntities.associate { it.invoiceId to it.roomName }
                Result.success(TicketsResult(tickets = remoteTickets, roomMap = roomMap, isOffline = false))
            } else {
                fallbackToCache(Exception(response.message.ifBlank { "Không thể tải danh sách vé" }))
            }
        } catch (e: Exception) {
            fallbackToCache(e)
        }
    }

    suspend fun getInvoiceDetail(invoiceId: String): Result<InvoiceResponse> = withContext(Dispatchers.IO) {
        try {
            val response = accountApi.getInvoiceDetail(invoiceId)
            if (response.success && response.data != null) {
                val detail = response.data
                val cached = ticketDao.getTicketById(invoiceId)
                if (cached != null && detail.roomName != null && cached.roomName != detail.roomName) {
                    ticketDao.insertTickets(listOf(cached.copy(roomName = detail.roomName)))
                }
                Result.success(detail)
            } else {
                fallbackInvoiceFromCache(invoiceId, Exception(response.message.ifBlank { "Không tìm thấy hóa đơn" }))
            }
        } catch (e: Exception) {
            fallbackInvoiceFromCache(invoiceId, e)
        }
    }

    private suspend fun fallbackToCache(error: Throwable): Result<TicketsResult> {
        val cached = ticketDao.getAllTickets()
        return if (cached.isNotEmpty()) {
            val tickets = cached.map { entity ->
                CustomerHistoryResponse(
                    invoiceId = entity.invoiceId,
                    movieTitle = entity.movieTitle,
                    showDate = entity.showDate,
                    startTime = entity.startTime,
                    totalAmount = entity.totalAmount,
                    status = entity.status,
                    paymentDate = entity.paymentDate,
                    ticketCount = entity.ticketCount,
                    checkedInCount = entity.checkedInCount,
                    seatIds = entity.seats.split(", ").filter { it.isNotBlank() }
                )
            }
            val roomMap = cached.associate { it.invoiceId to it.roomName }
            Result.success(TicketsResult(tickets = tickets, roomMap = roomMap, isOffline = true))
        } else {
            Result.failure(error)
        }
    }

    private suspend fun fallbackInvoiceFromCache(invoiceId: String, error: Throwable): Result<InvoiceResponse> {
        val cached = ticketDao.getTicketById(invoiceId)
        return if (cached != null) {
            val fallback = InvoiceResponse(
                transactionRef = cached.invoiceId,
                paymentId = cached.invoiceId,
                paymentStatus = cached.status ?: "Đã thanh toán",
                totalAmount = cached.totalAmount,
                movieTitle = cached.movieTitle,
                showDate = cached.showDate,
                startTime = cached.startTime,
                roomName = cached.roomName,
                seats = cached.seats.split(", ").filter { it.isNotBlank() }
            )
            Result.success(fallback)
        } else {
            Result.failure(error)
        }
    }
}
