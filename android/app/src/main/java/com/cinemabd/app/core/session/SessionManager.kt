package com.cinemabd.app.core.session

import com.cinemabd.app.core.datastore.TokenDataStore
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import java.util.concurrent.atomic.AtomicBoolean
import javax.inject.Inject
import javax.inject.Singleton

sealed interface SessionEvent {
    data object SessionExpired : SessionEvent
}

@Singleton
class SessionManager internal constructor(
    private val onClearSession: suspend () -> Unit
) {
    @Inject
    constructor(tokenDataStore: TokenDataStore) : this(
        onClearSession = { tokenDataStore.clearToken() }
    )

    private val _events = MutableSharedFlow<SessionEvent>(extraBufferCapacity = 1)
    val events: SharedFlow<SessionEvent> = _events.asSharedFlow()

    private val isExpiredEmitted = AtomicBoolean(false)

    suspend fun clearSession() {
        onClearSession()
    }

    suspend fun emitSessionExpired() {
        if (isExpiredEmitted.compareAndSet(false, true)) {
            _events.emit(SessionEvent.SessionExpired)
        }
    }

    fun resetSession() {
        isExpiredEmitted.set(false)
    }

    fun isSessionExpired(): Boolean = isExpiredEmitted.get()
}
