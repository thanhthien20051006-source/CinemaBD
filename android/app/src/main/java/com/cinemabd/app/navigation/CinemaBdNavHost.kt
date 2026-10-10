package com.cinemabd.app.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.cinemabd.app.feature.auth.AuthScreen
import com.cinemabd.app.feature.main.MainScreen

@Composable
fun CinemaBdNavHost(
    navController: NavHostController = rememberNavController(),
    startDestination: String = Routes.AUTH
) {
    NavHost(
        navController = navController,
        startDestination = startDestination
    ) {
        composable(Routes.AUTH) {
            AuthScreen(
                onAuthSuccess = {
                    navController.navigate(Routes.MAIN) {
                        popUpTo(Routes.AUTH) { inclusive = true }
                    }
                }
            )
        }
        composable(Routes.MAIN) {
            MainScreen(rootNavController = navController)
        }
        composable(
            Routes.MOVIE_DETAIL,
            arguments = listOf(navArgument("movieId") { type = NavType.StringType })
        ) {
            com.cinemabd.app.feature.movies.MovieDetailScreen(
                onBack = { navController.popBackStack() },
                onBookTicket = { movieId ->
                    navController.navigate(Routes.selectShowtimes(movieId))
                }
            )
        }
        composable(
            Routes.SELECT_SHOWTIMES,
            arguments = listOf(navArgument("movieId") { type = NavType.StringType })
        ) {
            com.cinemabd.app.feature.booking.SelectShowtimeScreen(
                onBack = { navController.popBackStack() },
                onSelectShowtime = { showtimeId ->
                    navController.navigate(Routes.selectSeats(showtimeId))
                }
            )
        }
        composable(
            Routes.SELECT_SEATS,
            arguments = listOf(navArgument("showtimeId") { type = NavType.StringType })
        ) {
            com.cinemabd.app.feature.booking.SelectSeatScreen(
                onBack = { navController.popBackStack() }
            )
        }
    }
}
