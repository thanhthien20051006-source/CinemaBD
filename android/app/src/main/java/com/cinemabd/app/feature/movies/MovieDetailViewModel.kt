package com.cinemabd.app.feature.movies

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.core.network.model.ReviewResponse
import com.cinemabd.app.data.repository.MoviesRepository
import com.cinemabd.app.data.repository.ReviewsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class MovieDetailUiState(
    val isLoading: Boolean = true,
    val movie: MovieResponse? = null,
    val reviews: List<ReviewResponse> = emptyList(),
    val error: String? = null
)

@HiltViewModel
class MovieDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val moviesRepository: MoviesRepository,
    private val reviewsRepository: ReviewsRepository
) : ViewModel() {

    private val movieId: String = checkNotNull(savedStateHandle["movieId"])

    private val _uiState = MutableStateFlow(MovieDetailUiState())
    val uiState: StateFlow<MovieDetailUiState> = _uiState.asStateFlow()

    init {
        loadData()
    }

    fun loadData() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            
            val movieDeferred = async { moviesRepository.getMovieById(movieId) }
            val reviewsDeferred = async { reviewsRepository.getReviewsByMovie(movieId) }
            
            val movieResult = movieDeferred.await()
            val reviewsResult = reviewsDeferred.await()
            
            if (movieResult.isSuccess) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        movie = movieResult.getOrNull(),
                        reviews = reviewsResult.getOrDefault(emptyList()),
                        error = null
                    )
                }
            } else {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        error = movieResult.exceptionOrNull()?.message ?: "An error occurred"
                    )
                }
            }
        }
    }
}
