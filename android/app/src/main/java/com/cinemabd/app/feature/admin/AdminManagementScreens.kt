package com.cinemabd.app.feature.admin

import android.widget.Toast
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.LifecycleResumeEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.compose.AsyncImage
import com.cinemabd.app.R
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.core.utils.DateTimeUtils
import com.cinemabd.app.core.utils.formatImageUrl
import com.cinemabd.app.feature.admin.model.AdminShowtime
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.Locale

@Composable
fun AdminShowtimeListScreen(
    viewModel: AdminShowtimeListViewModel = hiltViewModel(),
    onBack: (() -> Unit)? = null,
    onShowtimeClick: (String, String) -> Unit
) {
    LifecycleResumeEffect(Unit) {
        viewModel.loadShowtimes()
        onPauseOrDispose {}
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    Scaffold(topBar = { AdminTopBar(stringResource(R.string.admin_showtimes_title), onBack) }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            val selectedDate = when (val current = state) {
                is AdminShowtimeListUiState.Success -> LocalDate.parse(current.selectedDate)
                is AdminShowtimeListUiState.Empty -> LocalDate.parse(current.selectedDate)
                is AdminShowtimeListUiState.Loading -> LocalDate.parse(current.selectedDate)
                else -> LocalDate.now()
            }
            ShowtimeDateStrip(selectedDate, viewModel::selectDate)
            when (val current = state) {
                is AdminShowtimeListUiState.Loading -> LoadingContent()
                is AdminShowtimeListUiState.Error -> ErrorContent(current.message, viewModel::loadShowtimes)
                is AdminShowtimeListUiState.Empty -> EmptyContent(stringResource(R.string.admin_showtimes_empty), viewModel::loadShowtimes)
                is AdminShowtimeListUiState.Success -> LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(horizontal = 16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(current.showtimes, key = { it.id }) { showtime ->
                        ShowtimeCard(showtime) { onShowtimeClick(showtime.id, current.selectedDate) }
                    }
                }
            }
        }
    }
}

@Composable
private fun ShowtimeDateStrip(selectedDate: LocalDate, onDateClick: (LocalDate) -> Unit) {
    val dates = remember { (-3..10).map { LocalDate.now().plusDays(it.toLong()) } }
    androidx.compose.foundation.lazy.LazyRow(
        modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        items(dates) { date ->
            val selected = date == selectedDate
            OutlinedButton(
                onClick = { onDateClick(date) },
                modifier = Modifier.padding(
                    start = if (date == dates.first()) 16.dp else 0.dp,
                    end = if (date == dates.last()) 16.dp else 0.dp
                )
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(date.format(DateTimeFormatter.ofPattern("EEE", Locale.forLanguageTag("vi-VN"))))
                    Text(date.format(DateTimeFormatter.ofPattern("dd/MM")), fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal)
                }
            }
        }
    }
}

@Composable
private fun ShowtimeCard(showtime: AdminShowtime, onClick: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick),
        colors = CardDefaults.cardColors()
    ) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            val movieTitle = showtime.movieTitle ?: showtime.movieId
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text(
                    text = if (!movieTitle.isNullOrBlank()) movieTitle else stringResource(R.string.admin_showtime_code, showtime.id),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.weight(1f, fill = false)
                )
                Text(
                    text = showtime.status ?: stringResource(R.string.admin_status_unknown),
                    color = if (showtime.status == "Cancelled") MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.primary,
                    fontWeight = FontWeight.SemiBold
                )
            }
            if (!movieTitle.isNullOrBlank()) {
                Text(
                    text = stringResource(R.string.admin_showtime_code, showtime.id),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            Text(stringResource(R.string.admin_showtime_room, showtime.roomName))
            Text(stringResource(R.string.admin_showtime_datetime, formatShowtimeDate(showtime.showDate), showtime.startTime.substringBeforeLast(':', showtime.startTime)))
            Text(stringResource(R.string.admin_showtime_price, DateTimeUtils.formatCurrency(showtime.ticketPrice)))
            Text(
                stringResource(R.string.admin_showtime_seat_counts, showtime.availableSeats, showtime.totalSeats, showtime.heldSeats, showtime.soldSeats, showtime.checkedInSeats),
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
fun AdminShowtimeDetailScreen(
    viewModel: AdminShowtimeDetailViewModel = hiltViewModel(),
    onBack: () -> Unit
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val message by viewModel.message.collectAsStateWithLifecycle()
    val isCancelling by viewModel.isCancelling.collectAsStateWithLifecycle()
    val context = LocalContext.current
    var showCancelDialog by remember { mutableStateOf(false) }
    LaunchedEffect(message) {
        message?.let {
            Toast.makeText(context, it, Toast.LENGTH_LONG).show()
            viewModel.clearMessage()
        }
    }
    Scaffold(topBar = { AdminTopBar(stringResource(R.string.admin_showtime_detail_title), onBack) }) { padding ->
        when (val current = state) {
            AdminShowtimeDetailUiState.Loading -> Column(Modifier.fillMaxSize().padding(padding)) { LoadingContent() }
            AdminShowtimeDetailUiState.Empty -> Column(Modifier.fillMaxSize().padding(padding)) { EmptyContent(stringResource(R.string.admin_showtime_not_found), viewModel::loadDetail) }
            is AdminShowtimeDetailUiState.Error -> Column(Modifier.fillMaxSize().padding(padding)) { ErrorContent(current.message, viewModel::loadDetail) }
            is AdminShowtimeDetailUiState.Success -> Column(
                Modifier.fillMaxSize().padding(padding).verticalScroll(rememberScrollState()).padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Text(current.showtime.id, style = androidx.compose.material3.MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
                val movieTitle = current.showtime.movieTitle ?: current.showtime.movieId
                if (!movieTitle.isNullOrBlank()) {
                    AdminDetailInfoRow(stringResource(R.string.check_in_movie_label), movieTitle)
                }
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_status), current.showtime.status ?: stringResource(R.string.admin_status_unknown))
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_room_label), current.showtime.roomName)
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_datetime_label), DateTimeUtils.formatShowDateTime(current.showtime.showDate, current.showtime.startTime))
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_price_label), DateTimeUtils.formatCurrency(current.showtime.ticketPrice))
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_seats_total_label), current.showtime.totalSeats.toString())
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_seats_available_label), current.showtime.availableSeats.toString())
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_seats_held_label), current.showtime.heldSeats.toString())
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_seats_sold_label), current.showtime.soldSeats.toString())
                AdminDetailInfoRow(stringResource(R.string.admin_showtime_seats_checked_in_label), current.showtime.checkedInSeats.toString())
                Spacer(Modifier.height(8.dp))
                if (current.showtime.status != "Cancelled" && current.showtime.status != "Expired") {
                    Button(
                        onClick = { showCancelDialog = true },
                        enabled = !isCancelling,
                        modifier = Modifier.fillMaxWidth(),
                        colors = androidx.compose.material3.ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
                    ) {
                        if (isCancelling) CircularProgressIndicator(Modifier.size(18.dp), color = MaterialTheme.colorScheme.onError)
                        else Text(stringResource(R.string.admin_showtime_cancel))
                    }
                }
            }
        }
    }
    if (showCancelDialog) {
        AlertDialog(
            onDismissRequest = { showCancelDialog = false },
            title = { Text(stringResource(R.string.admin_showtime_cancel_confirm_title)) },
            text = { Text(stringResource(R.string.admin_showtime_cancel_confirm_message)) },
            confirmButton = {
                TextButton(onClick = { showCancelDialog = false; viewModel.cancelShowtime() }) {
                    Text(stringResource(R.string.admin_showtime_cancel))
                }
            },
            dismissButton = { TextButton(onClick = { showCancelDialog = false }) { Text(stringResource(R.string.profile_logout_cancel)) } }
        )
    }
}

@Composable
fun AdminMovieListScreen(
    viewModel: AdminMovieListViewModel = hiltViewModel(),
    onBack: (() -> Unit)? = null,
    onMovieClick: (String) -> Unit
) {
    LifecycleResumeEffect(Unit) {
        viewModel.loadMovies()
        onPauseOrDispose {}
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    var query by remember { mutableStateOf("") }
    Scaffold(topBar = { AdminTopBar(stringResource(R.string.admin_movies_title), onBack) }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            OutlinedTextField(
                value = query,
                onValueChange = { query = it; viewModel.onSearchChange(it) },
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                label = { Text(stringResource(R.string.admin_movies_search)) },
                leadingIcon = { androidx.compose.material3.Icon(Icons.Filled.Search, contentDescription = null) },
                singleLine = true
            )
            when (val current = state) {
                AdminMovieListUiState.Loading -> LoadingContent()
                is AdminMovieListUiState.Error -> ErrorContent(current.message, viewModel::loadMovies)
                is AdminMovieListUiState.Empty -> EmptyContent(stringResource(R.string.admin_movies_empty), viewModel::loadMovies)
                is AdminMovieListUiState.Success -> LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(horizontal = 16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(current.movies, key = { it.id }) { movie -> MovieCard(movie) { onMovieClick(movie.id) } }
                }
            }
        }
    }
}

@Composable
private fun MovieCard(movie: MovieResponse, onClick: () -> Unit) {
    Card(Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Row(Modifier.padding(12.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            AsyncImage(
                model = formatImageUrl(movie.posterUrl),
                contentDescription = stringResource(R.string.admin_movie_poster_description, movie.title),
                modifier = Modifier.size(width = 86.dp, height = 126.dp)
            )
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(movie.title, style = androidx.compose.material3.MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                Text(movie.genre ?: stringResource(R.string.admin_value_unavailable))
                Text(stringResource(R.string.movie_duration_format, movie.durationMinutes))
                Text(movie.status ?: stringResource(R.string.admin_status_unknown), color = androidx.compose.material3.MaterialTheme.colorScheme.primary)
            }
        }
    }
}

@Composable
fun AdminMovieDetailScreen(
    viewModel: AdminMovieDetailViewModel = hiltViewModel(),
    onBack: () -> Unit
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val message by viewModel.message.collectAsStateWithLifecycle()
    val context = LocalContext.current
    LaunchedEffect(message) {
        message?.let {
            Toast.makeText(context, it, Toast.LENGTH_LONG).show()
            viewModel.clearMessage()
        }
    }
    Scaffold(topBar = { AdminTopBar(stringResource(R.string.admin_movie_detail_title), onBack) }) { padding ->
        when (val current = state) {
            AdminMovieDetailUiState.Loading -> Column(Modifier.fillMaxSize().padding(padding)) { LoadingContent() }
            AdminMovieDetailUiState.Empty -> Column(Modifier.fillMaxSize().padding(padding)) { EmptyContent(stringResource(R.string.movie_not_found), viewModel::loadDetail) }
            is AdminMovieDetailUiState.Error -> Column(Modifier.fillMaxSize().padding(padding)) { ErrorContent(current.message, viewModel::loadDetail) }
            is AdminMovieDetailUiState.Success -> MovieDetailContent(current.movie, current.isSaving, viewModel::updateStatus, Modifier.padding(padding))
        }
    }
}

@Composable
private fun MovieDetailContent(movie: MovieResponse, isSaving: Boolean, onStatusChange: (String) -> Unit, modifier: Modifier = Modifier) {
    Column(modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        AsyncImage(
            model = formatImageUrl(movie.posterUrl),
            contentDescription = stringResource(R.string.admin_movie_poster_description, movie.title),
            modifier = Modifier.fillMaxWidth().height(280.dp)
        )
        Text(movie.title, style = androidx.compose.material3.MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        AdminDetailInfoRow(stringResource(R.string.movie_genre), movie.genre ?: stringResource(R.string.admin_value_unavailable))
        AdminDetailInfoRow(stringResource(R.string.movie_duration), stringResource(R.string.movie_duration_format, movie.durationMinutes))
        AdminDetailInfoRow(stringResource(R.string.admin_movie_status), movie.status ?: stringResource(R.string.admin_status_unknown))
        if (!movie.director.isNullOrBlank()) AdminDetailInfoRow(stringResource(R.string.admin_movie_director), movie.director)
        if (!movie.country.isNullOrBlank()) AdminDetailInfoRow(stringResource(R.string.admin_movie_country), movie.country)
        if (!movie.description.isNullOrBlank()) Text(movie.description)
        Text(stringResource(R.string.admin_movie_change_status), fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 8.dp))
        val statuses = listOf(
            stringResource(R.string.admin_movie_status_now_showing),
            stringResource(R.string.admin_movie_status_coming_soon),
            stringResource(R.string.admin_movie_status_inactive)
        )
        statuses.forEach { status ->
            OutlinedButton(
                onClick = { onStatusChange(status) },
                enabled = !isSaving && movie.status != status,
                modifier = Modifier.fillMaxWidth()
            ) { Text(status) }
        }
        if (isSaving) CircularProgressIndicator(Modifier.align(Alignment.CenterHorizontally))
    }
}

@Composable
private fun AdminDetailInfoRow(label: String, value: String) {
    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, color = androidx.compose.material3.MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, fontWeight = FontWeight.Medium)
    }
}

private fun formatShowtimeDate(value: String): String = try {
    LocalDate.parse(value.substringBefore('T')).format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
} catch (_: Exception) { value }
