package com.cinemabd.app.feature.hello

data class HelloUiState(
    val isLoading: Boolean = false,
    val isSuccess: Boolean = true,
    val isError: Boolean = false,
    val errorMessage: String? = null
)
