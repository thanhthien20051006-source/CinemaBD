package com.cinemabd.app.data.repository

import com.cinemabd.app.core.database.dao.TicketDao
import com.cinemabd.app.core.database.entity.TicketDetailEntity
import com.cinemabd.app.core.database.entity.TicketEntity
import com.cinemabd.app.core.datastore.TokenDataStore
import com.cinemabd.app.core.network.AccountApi
import com.cinemabd.app.core.session.UserRole
import com.cinemabd.app.core.session.UserSession
import com.cinemabd.app.core.network.model.CustomerHistoryResponse
import com.cinemabd.app.core.network.model.CustomerTicketResponse
import com.cinemabd.app.core.network.model.InvoiceResponse
import com.cinemabd.app.core.network.model.VoucherPreviewRequest
import com.cinemabd.app.core.network.model.VoucherPreviewResponse
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.withContext
import java.time.Instant
import javax.inject.Inject
import javax.inject.Singleton

data class TicketsResult(
    val tickets: List<CustomerHistoryResponse>,
    val roomMap: Map<String, String?> = emptyMap(),
    val isOffline: Boolean = false
)

@OptIn(ExperimentalCoroutinesApi::class)
@Singleton
class TicketsRepository @Inject constructor(
    private val accountApi: AccountApi,
    private val ticketDao: TicketDao,
    private val tokenDataStore: TokenDataStore
) {

    fun getTicketsFlow(): Flow<List<TicketEntity>> {
        return tokenDataStore.sessionFlow.flatMapLatest { session ->
            val userId = session?.userId
            if (!userId.isNullOrBlank()) {
                ticketDao.getAllTickets(userId)
            } else {
                flowOf(emptyList())
            }
        }
    }

    fun getTicketDetailsFlow(invoiceId: String): Flow<List<TicketDetailEntity>> {
        return tokenDataStore.sessionFlow.flatMapLatest { session ->
            val userId = session?.userId
            if (!userId.isNullOrBlank()) {
                ticketDao.getTicketDetailsFlow(userId, invoiceId)
            } else {
                flowOf(emptyList())
            }
        }
    }

    suspend fun getTicketDetailsOnce(invoiceId: String): List<TicketDetailEntity> = withContext(Dispatchers.IO) {
        val session = currentCustomerSession() ?: return@withContext emptyList()
        ticketDao.getTicketDetails(session.userId!!, invoiceId)
    }

    suspend fun getMyTicketPayloads(): Result<List<CustomerTicketResponse>> = withContext(Dispatchers.IO) {
        val session = currentCustomerSession() ?: return@withContext Result.failure(IllegalStateException("Phiên khách hàng không hợp lệ"))
        try {
            val response = accountApi.getMyTickets()
            if (response.success && response.data != null) {
                ticketDao.upsertTicketDetails(response.data.map { it.toEntity(session.userId!!) })
                // Cập nhật qrPayload vào bảng tickets nếu đã tồn tại
                val existingTickets = ticketDao.getAllTicketsOnce(session.userId!!)
                if (existingTickets.isNotEmpty()) {
                    val qrMap = response.data.associateBy { it.transactionRef }
                    val updatedTickets = existingTickets.map { ticket ->
                        val qr = qrMap[ticket.invoiceId]?.qrPayload
                        if (!qr.isNullOrBlank() && ticket.qrPayload != qr) {
                            ticket.copy(qrPayload = qr)
                        } else {
                            ticket
                        }
                    }
                    ticketDao.upsertTickets(updatedTickets)
                }
                Result.success(response.data)
            } else {
                fallbackTicketPayloads(
                    session.userId!!,
                    Exception(response.message.ifBlank { "Không thể tải danh sách vé" })
                )
            }
        } catch (e: Exception) {
            fallbackTicketPayloads(session.userId!!, e)
        }
    }

    suspend fun previewVoucher(
        showtimeId: String,
        seats: List<String>,
        combos: String?,
        code: String
    ): Result<VoucherPreviewResponse> = withContext(Dispatchers.IO) {
        try {
            val response = accountApi.previewVoucher(
                VoucherPreviewRequest(showtimeId, seats, combos, code)
            )
            if (response.success && response.data != null && response.data.success) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.data?.message?.takeIf(String::isNotBlank)
                    ?: response.message.ifBlank { "Không thể áp dụng voucher" }))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getTickets(): Result<TicketsResult> = withContext(Dispatchers.IO) {
        val session = currentCustomerSession()
            ?: return@withContext Result.failure(IllegalStateException("Không có phiên khách hàng để tải vé"))
        val userId = session.userId!!
        try {
            val response = accountApi.getHistory()
            if (response.success && response.data != null) {
                val remoteTickets = response.data
                val existingEntities = ticketDao.getAllTicketsOnce(userId).associateBy { it.invoiceId }
                val signedDetails = ticketDao.getTicketDetailsForUser(userId).associateBy { it.invoiceId }

                val newEntities = remoteTickets.map { dto ->
                    val existing = existingEntities[dto.invoiceId]
                    val qr = signedDetails[dto.invoiceId]?.qrPayload ?: existing?.qrPayload
                    TicketEntity(
                        userId = userId,
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
                        checkedInCount = dto.checkedInCount,
                        qrPayload = qr
                    )
                }

                ticketDao.upsertTickets(newEntities)

                val roomMap = newEntities.associate { it.invoiceId to it.roomName }
                Result.success(TicketsResult(tickets = remoteTickets, roomMap = roomMap, isOffline = false))
            } else {
                fallbackToCache(userId, Exception(response.message.ifBlank { "Không thể tải danh sách vé" }))
            }
        } catch (e: Exception) {
            fallbackToCache(userId, e)
        }
    }

    suspend fun getInvoiceDetail(invoiceId: String): Result<InvoiceResponse> = withContext(Dispatchers.IO) {
        val session = currentCustomerSession()
            ?: return@withContext Result.failure(IllegalStateException("Không có phiên khách hàng để tải vé"))
        val userId = session.userId!!
        try {
            val response = accountApi.getInvoiceDetail(invoiceId)
            if (response.success && response.data != null) {
                val detail = response.data
                val cached = ticketDao.getTicketByIdOnce(userId, invoiceId)
                if (cached != null && detail.roomName != null && cached.roomName != detail.roomName) {
                    ticketDao.upsertTickets(listOf(cached.copy(roomName = detail.roomName)))
                }
                ticketDao.upsertTicketDetails(
                    detail.tickets.mapNotNull { ticket ->
                        val signed = signedTicketFor(userId, invoiceId, ticket.ticketId)
                        signed?.copy(
                            movieTitle = detail.movieTitle,
                            roomName = detail.roomName,
                            showDate = detail.showDate,
                            startTime = detail.startTime,
                            seatId = ticket.seatId,
                            price = ticket.price,
                            status = ticket.status,
                            isCheckedIn = ticket.isCheckedIn,
                            checkedInAt = ticket.checkedInAt
                        )
                    }
                )
                Result.success(detail)
            } else {
                fallbackInvoiceFromCache(userId, invoiceId, Exception(response.message.ifBlank { "Không tìm thấy hóa đơn" }))
            }
        } catch (e: Exception) {
            fallbackInvoiceFromCache(userId, invoiceId, e)
        }
    }

    private suspend fun fallbackToCache(userId: String, error: Throwable): Result<TicketsResult> {
        val cached = ticketDao.getAllTicketsOnce(userId)
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

    private suspend fun fallbackInvoiceFromCache(userId: String, invoiceId: String, error: Throwable): Result<InvoiceResponse> {
        val cached = ticketDao.getTicketByIdOnce(userId, invoiceId)
        return if (cached != null) {
            val cachedDetails = ticketDao.getTicketDetails(userId, invoiceId)
            val fallback = InvoiceResponse(
                transactionRef = cached.invoiceId,
                paymentId = cached.invoiceId,
                paymentStatus = cached.status ?: "Đã thanh toán",
                totalAmount = cached.totalAmount,
                movieTitle = cached.movieTitle,
                showDate = cached.showDate,
                startTime = cached.startTime,
                roomName = cached.roomName,
                seats = cached.seats.split(", ").filter { it.isNotBlank() },
                tickets = cachedDetails.map { detail ->
                    com.cinemabd.app.core.network.model.InvoiceTicketResponse(
                        ticketId = detail.ticketId,
                        seatId = detail.seatId,
                        price = detail.price,
                        status = detail.status,
                        isCheckedIn = detail.isCheckedIn,
                        checkedInAt = detail.checkedInAt
                    )
                }
            )
            Result.success(fallback)
        } else {
            Result.failure(error)
        }
    }

    private suspend fun fallbackTicketPayloads(
        userId: String,
        error: Throwable
    ): Result<List<CustomerTicketResponse>> {
        val cached = ticketDao.getTicketDetailsForUser(userId)
        return if (cached.isNotEmpty()) {
            Result.success(cached.map { it.toResponse() })
        } else {
            Result.failure(error)
        }
    }

    private suspend fun currentCustomerSession(): UserSession? {
        val session = tokenDataStore.sessionFlow.first() ?: return null
        return session.takeIf {
            it.role == UserRole.Customer &&
                !it.userId.isNullOrBlank() &&
                it.expiresAtUtcMillis > System.currentTimeMillis()
        }
    }

    private fun CustomerTicketResponse.toEntity(userId: String) = TicketDetailEntity(
        ticketId = ticketId,
        userId = userId,
        invoiceId = transactionRef,
        movieTitle = movieTitle,
        roomName = roomName,
        showDate = showDate,
        startTime = startTime,
        seatId = seatId,
        price = price,
        status = status,
        isCheckedIn = !checkedInAt.isNullOrBlank(),
        checkedInAt = checkedInAt,
        qrPayload = qrPayload,
        qrSignature = qrSignature,
        qrIssuedAtUtc = qrIssuedAtUtc
    )

    private fun TicketDetailEntity.toResponse() = CustomerTicketResponse(
        ticketId = ticketId,
        transactionRef = invoiceId,
        movieTitle = movieTitle.orEmpty(),
        moviePosterUrl = "",
        showDate = showDate.orEmpty(),
        startTime = startTime.orEmpty(),
        roomName = roomName.orEmpty(),
        seatId = seatId,
        price = price,
        status = status.orEmpty(),
        checkedInAt = checkedInAt,
        qrPayload = qrPayload,
        qrSignature = qrSignature,
        qrIssuedAtUtc = qrIssuedAtUtc.orEmpty()
    )

    private suspend fun signedTicketFor(userId: String, invoiceId: String, ticketId: String): TicketDetailEntity? =
        ticketDao.getTicketDetails(userId, invoiceId).firstOrNull { it.ticketId == ticketId }
}
