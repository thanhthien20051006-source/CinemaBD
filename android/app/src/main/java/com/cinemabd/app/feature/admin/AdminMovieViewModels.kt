package com.cinemabd.app.feature.admin

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.toUserFriendlyMessage
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class AdminMovieListViewModel @Inject constructor(
    private val repository: AdminRepository
) : ViewModel() {
    private val _uiState = MutableStateFlow<AdminMovieListUiState>(AdminMovieListUiState.Loading)
    val uiState: StateFlow<AdminMovieListUiState> = _uiState.asStateFlow()
    private var searchJob: Job? = null
    private var query = ""

    init { loadMovies() }

    fun onSearchChange(value: String) {
        query = value
        loadMovies()
    }

    fun loadMovies() {
        val currentQuery = query.trim()
        searchJob?.cancel()
        searchJob = viewModelScope.launch {
            _uiState.value = AdminMovieListUiState.Loading
            repository.getMovies(currentQuery)
                .onSuccess { response ->
                    if (!response.success || response.data == null) {
                        _uiState.value = AdminMovieListUiState.Error(
                            response.message.ifBlank { "Không thể tải danh sách phim." }
                        )
                    } else {
                        val movies = response.data.filter {
                            currentQuery.isBlank() || it.title.contains(currentQuery, ignoreCase = true)
                        }
                        _uiState.value = if (movies.isEmpty()) {
                            AdminMovieListUiState.Empty(currentQuery)
                        } else {
                            AdminMovieListUiState.Success(currentQuery, movies)
                        }
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminMovieListUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải danh sách phim.")
                    )
                }
        }
    }
}

@HiltViewModel
class AdminMovieDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val repository: AdminRepository
) : ViewModel() {
    private val movieId: String = checkNotNull(savedStateHandle["movieId"])
    private val _uiState = MutableStateFlow<AdminMovieDetailUiState>(AdminMovieDetailUiState.Loading)
    val uiState: StateFlow<AdminMovieDetailUiState> = _uiState.asStateFlow()
    private val _message = MutableStateFlow<String?>(null)
    val message: StateFlow<String?> = _message.asStateFlow()

    init { loadDetail() }

    fun loadDetail() {
        viewModelScope.launch {
            _uiState.value = AdminMovieDetailUiState.Loading
            repository.getMovie(movieId)
                .onSuccess { response ->
                    _uiState.value = when {
                        !response.success -> AdminMovieDetailUiState.Error(
                            response.message.ifBlank { "Không thể tải chi tiết phim." }
                        )
                        response.data == null -> AdminMovieDetailUiState.Empty
                        else -> AdminMovieDetailUiState.Success(response.data)
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminMovieDetailUiState.Error(
                        error.toUserFriendlyMessage("Không thể tải chi tiết phim.")
                    )
                }
        }
    }

    fun updateStatus(status: String) {
        val current = (_uiState.value as? AdminMovieDetailUiState.Success)?.movie ?: return
        viewModelScope.launch {
            _uiState.value = AdminMovieDetailUiState.Success(current, isSaving = true)
            repository.updateMovieStatus(movieId, status)
                .onSuccess { response ->
                    if (response.success && response.data != null) {
                        _uiState.value = AdminMovieDetailUiState.Success(response.data)
                        _message.value = response.message.ifBlank { "Đã cập nhật trạng thái phim thành công." }
                    } else {
                        _uiState.value = AdminMovieDetailUiState.Success(current)
                        _message.value = response.message.ifBlank { "Không thể cập nhật trạng thái phim." }
                    }
                }
                .onFailure { error ->
                    _uiState.value = AdminMovieDetailUiState.Success(current)
                    _message.value = error.toUserFriendlyMessage("Không thể cập nhật trạng thái phim.")
                }
        }
    }

    fun clearMessage() { _message.update { null } }
}
