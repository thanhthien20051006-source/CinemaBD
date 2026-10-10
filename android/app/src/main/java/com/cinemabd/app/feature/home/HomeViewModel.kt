package com.cinemabd.app.feature.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.data.repository.MoviesRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface HomeUiState {
    data object Loading : HomeUiState
    data object Error : HomeUiState
    data object Empty : HomeUiState
    data class Success(
        val bannerMovies: List<MovieResponse>,
        val showingMovies: List<MovieResponse>,
        val upcomingMovies: List<MovieResponse>
    ) : HomeUiState
}

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val moviesRepository: MoviesRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<HomeUiState>(HomeUiState.Loading)
    val uiState: StateFlow<HomeUiState> = _uiState.asStateFlow()

    init {
        fetchMovies()
    }

    fun fetchMovies() {
        viewModelScope.launch {
            _uiState.update { HomeUiState.Loading }
            val result = moviesRepository.getMovies()
            result.onSuccess { movies ->
                if (movies.isEmpty()) {
                    _uiState.update { HomeUiState.Empty }
                } else {
                    val showing = movies.filter { it.status.equals("Đang chiếu", ignoreCase = true) || it.status.equals("Dang chieu", ignoreCase = true) }
                    val upcoming = movies.filter { it.status.equals("Sắp chiếu", ignoreCase = true) || it.status.equals("Sap chieu", ignoreCase = true) }
                    
                    val showingFinal = showing.ifEmpty { movies }
                    val upcomingFinal = upcoming.ifEmpty { movies.filterNot { showingFinal.contains(it) } }
                    
                    val bannerMovies = showingFinal.take(5)
                    _uiState.update { 
                        HomeUiState.Success(
                            bannerMovies = bannerMovies,
                            showingMovies = showingFinal,
                            upcomingMovies = upcomingFinal
                        )
                    }
                }
            }.onFailure {
                _uiState.update { HomeUiState.Error }
            }
        }
    }
}
