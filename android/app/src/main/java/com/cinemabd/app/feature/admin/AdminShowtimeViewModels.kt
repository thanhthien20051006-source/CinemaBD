package com.cinemabd.app.feature.admin

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import com.cinemabd.app.feature.admin.model.AdminShowtime
import dagger.hilt.android.lifecycle.HiltViewModel
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class AdminShowtimeListViewModel @Inject constructor(
    private val repository: AdminRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow<AdminShowtimeListUiState>(AdminShowtimeListUiState.Loading(LocalDate.now().toString()))
    val uiState: StateFlow<AdminShowtimeListUiState> = _uiState.asStateFlow()
    private var selectedDate = LocalDate.now()

    init { loadShowtimes() }

    fun selectDate(date: LocalDate) {
        if (selectedDate == date) return
        selectedDate = date
        loadShowtimes()
    }

    fun loadShowtimes() {
        val date = selectedDate
        viewModelScope.launch {
            _uiState.value = AdminShowtimeListUiState.Loading(date.toString())
            repository.getShowtimes(date.format(DateTimeFormatter.ISO_LOCAL_DATE))
                .onSuccess { response ->
                    if (!response.success || response.data == null) {
                        _uiState.value = AdminShowtimeListUiState.Error(
                            response.message.ifBlank { "Không thể tải danh sách suất chiếu." }
                        )
                    } else if (response.data.isEmpty()) {
                        _uiState.value = AdminShowtimeListUiState.Empty(date.toString())
                    } else {
                        _uiState.value = AdminShowtimeListUiState.Success(date.toString(), response.data)
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminShowtimeListUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải danh sách suất chiếu.")
                    )
                }
        }
    }
}

@HiltViewModel
class AdminShowtimeDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val repository: AdminRepository
) : ViewModel() {
    private val showtimeId: String = checkNotNull(savedStateHandle["showtimeId"])
    private val showDate: String = checkNotNull(savedStateHandle["showDate"])
    private val _uiState = MutableStateFlow<AdminShowtimeDetailUiState>(AdminShowtimeDetailUiState.Loading)
    val uiState: StateFlow<AdminShowtimeDetailUiState> = _uiState.asStateFlow()
    private val _message = MutableStateFlow<String?>(null)
    val message: StateFlow<String?> = _message.asStateFlow()
    private val _isCancelling = MutableStateFlow(false)
    val isCancelling: StateFlow<Boolean> = _isCancelling.asStateFlow()

    init { loadDetail() }

    fun loadDetail() {
        viewModelScope.launch {
            _uiState.value = AdminShowtimeDetailUiState.Loading
            repository.getShowtimes(showDate)
                .onSuccess { response ->
                    val showtime = response.data?.firstOrNull { it.id == showtimeId }
                    _uiState.value = when {
                        !response.success -> AdminShowtimeDetailUiState.Error(
                            response.message.ifBlank { "Không thể tải chi tiết suất chiếu." }
                        )
                        showtime == null -> AdminShowtimeDetailUiState.Empty
                        else -> AdminShowtimeDetailUiState.Success(showtime)
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminShowtimeDetailUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải chi tiết suất chiếu.")
                    )
                }
        }
    }

    fun cancelShowtime(onSuccess: () -> Unit = {}) {
        if (_isCancelling.value) return
        viewModelScope.launch {
            _isCancelling.value = true
            repository.cancelShowtime(showtimeId)
                .onSuccess { response ->
                    if (response.success) {
                        val current = (_uiState.value as? AdminShowtimeDetailUiState.Success)?.showtime
                        if (current != null) {
                            _uiState.value = AdminShowtimeDetailUiState.Success(current.copy(status = "Cancelled"))
                        }
                        _message.value = response.message.ifBlank { "Đã hủy suất chiếu thành công." }
                        onSuccess()
                    } else {
                        _message.value = response.message.ifBlank { "Không thể hủy suất chiếu." }
                    }
                }
                .onFailure { error ->
                    _message.value = error.toUserFriendlyMessage("Không thể hủy suất chiếu.")
                }
            _isCancelling.value = false
        }
    }

    fun clearMessage() { _message.update { null } }
}
