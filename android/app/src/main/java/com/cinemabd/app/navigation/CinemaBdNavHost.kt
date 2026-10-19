package com.cinemabd.app.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.navigation
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import androidx.navigation.navDeepLink
import com.cinemabd.app.feature.auth.AuthScreen
import com.cinemabd.app.feature.booking.BookingSharedViewModel
import com.cinemabd.app.feature.booking.CheckoutScreen
import com.cinemabd.app.feature.booking.PaymentSuccessScreen
import com.cinemabd.app.feature.booking.SelectComboScreen
import com.cinemabd.app.feature.booking.SelectSeatScreen
import com.cinemabd.app.feature.booking.SelectShowtimeScreen
import com.cinemabd.app.feature.main.MainScreen
import com.cinemabd.app.feature.movies.MovieDetailScreen
import com.cinemabd.app.feature.tickets.TicketDetailScreen

@Composable
fun CinemaBdNavHost(
    navController: NavHostController = rememberNavController(),
    startDestination: String = Routes.AUTH,
    isAdmin: Boolean = false
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
        if (!isAdmin) {
            composable(
                Routes.MOVIE_DETAIL,
                arguments = listOf(navArgument("movieId") { type = NavType.StringType })
            ) {
                MovieDetailScreen(
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
                SelectShowtimeScreen(
                    onBack = { navController.popBackStack() },
                    onSelectShowtime = { showtimeId ->
                        navController.navigate(Routes.selectSeats(showtimeId))
                    }
                )
            }
            navigation(
                route = Routes.BOOKING_GRAPH,
                startDestination = Routes.SELECT_SEATS
            ) {
                composable(
                    route = Routes.SELECT_SEATS,
                    arguments = listOf(navArgument("showtimeId") { type = NavType.StringType })
                ) { backStackEntry ->
                    val showtimeId = backStackEntry.arguments?.getString("showtimeId") ?: ""
                    val parentEntry = remember(backStackEntry) {
                        navController.getBackStackEntry(Routes.BOOKING_GRAPH)
                    }
                    val bookingSharedViewModel: BookingSharedViewModel = hiltViewModel(parentEntry)

                    SelectSeatScreen(
                        onBack = { navController.popBackStack() },
                        onContinue = { sid ->
                            navController.navigate(Routes.selectCombo(sid))
                        },
                        showtimeId = showtimeId,
                        bookingSharedViewModel = bookingSharedViewModel
                    )
                }
                composable(
                    route = Routes.SELECT_COMBO,
                    arguments = listOf(navArgument("showtimeId") { type = NavType.StringType })
                ) { backStackEntry ->
                    val showtimeId = backStackEntry.arguments?.getString("showtimeId") ?: ""
                    val parentEntry = remember(backStackEntry) {
                        navController.getBackStackEntry(Routes.BOOKING_GRAPH)
                    }
                    val bookingSharedViewModel: BookingSharedViewModel = hiltViewModel(parentEntry)

                    SelectComboScreen(
                        onBack = { navController.popBackStack() },
                        onContinue = {
                            navController.navigate(Routes.checkout(showtimeId))
                        },
                        showtimeId = showtimeId,
                        bookingSharedViewModel = bookingSharedViewModel
                    )
                }
                composable(
                    route = Routes.CHECKOUT,
                    arguments = listOf(navArgument("showtimeId") { type = NavType.StringType })
                ) { backStackEntry ->
                    val showtimeId = backStackEntry.arguments?.getString("showtimeId") ?: ""
                    val parentEntry = remember(backStackEntry) {
                        navController.getBackStackEntry(Routes.BOOKING_GRAPH)
                    }
                    val bookingSharedViewModel: BookingSharedViewModel = hiltViewModel(parentEntry)

                    CheckoutScreen(
                        onBack = { navController.popBackStack() },
                        onPaymentSuccess = { txnRef ->
                            bookingSharedViewModel.clearCart()
                            navController.navigate(Routes.paymentSuccess(txnRef)) {
                                popUpTo(Routes.BOOKING_GRAPH) { inclusive = true }
                            }
                        },
                        showtimeId = showtimeId,
                        bookingSharedViewModel = bookingSharedViewModel
                    )
                }
            }
            composable(
                Routes.PAYMENT_SUCCESS,
                arguments = listOf(navArgument("txnRef") { type = NavType.StringType }),
                deepLinks = listOf(
                    navDeepLink { uriPattern = "cinemabd://payment-result?txnRef={txnRef}" }
                )
            ) {
                PaymentSuccessScreen(
                    onBackToHome = {
                        navController.navigate(Routes.MAIN) {
                            popUpTo(Routes.MAIN) { inclusive = true }
                        }
                    }
                )
            }
            composable(
                route = Routes.TICKET_DETAIL,
                arguments = listOf(navArgument("invoiceId") { type = NavType.StringType })
            ) {
                TicketDetailScreen(
                    onBack = { navController.popBackStack() }
                )
            }
        }
    }
}
