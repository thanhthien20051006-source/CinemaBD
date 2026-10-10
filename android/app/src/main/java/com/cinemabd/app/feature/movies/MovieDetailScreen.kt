package com.cinemabd.app.feature.movies

import androidx.compose.animation.animateContentSize
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import coil.compose.AsyncImage
import com.cinemabd.app.R
import com.cinemabd.app.core.network.model.MovieResponse
import com.cinemabd.app.core.network.model.ReviewResponse
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MovieDetailScreen(
    onBack: () -> Unit,
    onBookTicket: (String) -> Unit,
    viewModel: MovieDetailViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(id = R.string.movie_detail_title)) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                )
            )
        },
        floatingActionButton = {
            if (uiState.movie != null) {
                ExtendedFloatingActionButton(
                    onClick = { onBookTicket(uiState.movie!!.id) },
                    containerColor = MaterialTheme.colorScheme.primary,
                    contentColor = MaterialTheme.colorScheme.onPrimary
                ) {
                    Text(text = stringResource(id = R.string.book_ticket_btn), fontWeight = FontWeight.Bold)
                }
            }
        },
        floatingActionButtonPosition = FabPosition.Center
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when {
                uiState.isLoading -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }
                uiState.error != null -> {
                    Column(
                        modifier = Modifier.align(Alignment.Center),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(text = uiState.error ?: stringResource(id = R.string.error))
                        Spacer(modifier = Modifier.height(8.dp))
                        Button(onClick = { viewModel.loadData() }) {
                            Text(stringResource(id = R.string.retry))
                        }
                    }
                }
                uiState.movie != null -> {
                    MovieDetailContent(
                        movie = uiState.movie!!,
                        reviews = uiState.reviews,
                        modifier = Modifier.fillMaxSize()
                    )
                }
            }
        }
    }
}

@Composable
fun MovieDetailContent(
    movie: MovieResponse,
    reviews: List<ReviewResponse>,
    modifier: Modifier = Modifier
) {
    LazyColumn(
        modifier = modifier,
        contentPadding = PaddingValues(bottom = 80.dp) // padding for FAB
    ) {
        item {
            AsyncImage(
                model = movie.posterUrl,
                contentDescription = movie.title,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(400.dp),
                contentScale = ContentScale.Crop
            )
        }

        item {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = movie.title,
                    style = MaterialTheme.typography.headlineMedium,
                    fontWeight = FontWeight.Bold
                )

                Spacer(modifier = Modifier.height(8.dp))

                if (!movie.genre.isNullOrBlank()) {
                    Text(text = "${stringResource(id = R.string.movie_genre)} ${movie.genre}")
                }

                Text(text = "${stringResource(id = R.string.movie_duration)} ${String.format(stringResource(id = R.string.movie_duration_format), movie.durationMinutes)}")

                if (!movie.releaseDate.isNullOrBlank()) {
                    Text(text = "${stringResource(id = R.string.movie_release_date)} ${formatDate(movie.releaseDate)}")
                }

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = stringResource(id = R.string.movie_description),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(modifier = Modifier.height(4.dp))

                var isDescriptionExpanded by remember { mutableStateOf(false) }

                Column(modifier = Modifier.animateContentSize()) {
                    Text(
                        text = movie.description ?: "",
                        style = MaterialTheme.typography.bodyMedium,
                        maxLines = if (isDescriptionExpanded) Int.MAX_VALUE else 3,
                        overflow = if (isDescriptionExpanded) TextOverflow.Clip else TextOverflow.Ellipsis
                    )

                    if (!movie.description.isNullOrBlank() && movie.description.length > 100) {
                        TextButton(
                            onClick = { isDescriptionExpanded = !isDescriptionExpanded },
                            modifier = Modifier.align(Alignment.End)
                        ) {
                            Text(
                                text = if (isDescriptionExpanded)
                                    stringResource(id = R.string.collapse_text)
                                else
                                    stringResource(id = R.string.expand_text)
                            )
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = stringResource(id = R.string.movie_reviews),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(modifier = Modifier.height(8.dp))

                if (reviews.isEmpty()) {
                    Text(
                        text = stringResource(id = R.string.empty_reviews),
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }

        items(reviews) { review ->
            ReviewItem(review = review)
        }
    }
}

@Composable
fun ReviewItem(review: ReviewResponse) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 4.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = review.customerName ?: "Người dùng ẩn danh",
                    fontWeight = FontWeight.SemiBold,
                    style = MaterialTheme.typography.titleSmall
                )

                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = "${review.rating}",
                        fontWeight = FontWeight.Bold,
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Icon(
                        imageVector = Icons.Default.Star,
                        contentDescription = "Star",
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(16.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(4.dp))

            Text(
                text = review.content,
                style = MaterialTheme.typography.bodyMedium
            )

            Spacer(modifier = Modifier.height(4.dp))

            Text(
                text = formatDate(review.createdAt),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

private fun formatDate(dateString: String): String {
    try {
        val parser = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US)
        parser.timeZone = TimeZone.getTimeZone("UTC")
        val date = parser.parse(dateString) ?: return dateString

        val formatter = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault())
        return formatter.format(date)
    } catch (e: Exception) {
        return dateString
    }
}