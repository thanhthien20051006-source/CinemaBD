package com.cinemabd.app.feature.main

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Done
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavType
import androidx.navigation.navArgument
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navDeepLink
import com.cinemabd.app.feature.admin.AdminComboScreen
import com.cinemabd.app.feature.admin.AdminDashboardScreen
import com.cinemabd.app.feature.admin.AdminManagementScreen
import com.cinemabd.app.feature.admin.AdminMovieDetailScreen
import com.cinemabd.app.feature.admin.AdminMovieListScreen
import com.cinemabd.app.feature.admin.AdminRefundScreen
import com.cinemabd.app.feature.admin.AdminShowtimeDetailScreen
import com.cinemabd.app.feature.admin.AdminShowtimeListScreen
import com.cinemabd.app.feature.admin.AdminVoucherScreen
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.NavHostController
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.R
import com.cinemabd.app.feature.admin.scanner.AdminQrScannerScreen
import com.cinemabd.app.feature.admin.InvoiceDetailScreen
import com.cinemabd.app.feature.admin.InvoiceListScreen
import com.cinemabd.app.feature.admin.StatisticsScreen
import com.cinemabd.app.feature.home.HomeScreen
import com.cinemabd.app.feature.profile.ProfileScreen
import com.cinemabd.app.feature.tickets.MyTicketsScreen
import com.cinemabd.app.feature.tickets.TicketsScreen
import com.cinemabd.app.navigation.Routes

@Composable
fun MainScreen(
    rootNavController: NavHostController,
    initialRoute: String? = null,
    viewModel: MainScreenViewModel = hiltViewModel()
) {
    val navController = rememberNavController()
    val isAdmin by viewModel.isAdmin.collectAsStateWithLifecycle()
    val adminName by viewModel.adminName.collectAsStateWithLifecycle()

    if (isAdmin == null) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }
    val isAdminUser = isAdmin == true
    val adminDisplayName = adminName?.takeIf { it.isNotBlank() } ?: stringResource(R.string.admin_default_role)

    val handleAdminLogout: () -> Unit = {
        rootNavController.navigate(Routes.AUTH) {
            popUpTo(rootNavController.graph.id) { inclusive = true }
            launchSingleTop = true
        }
        viewModel.logout()
    }

    Scaffold(
        bottomBar = {
            NavigationBar {
                val navBackStackEntry by navController.currentBackStackEntryAsState()
                val currentDestination = navBackStackEntry?.destination

                val isCustomerDestination = currentDestination?.hierarchy?.any {
                    it.route in listOf(Routes.HOME, Routes.MY_TICKETS, Routes.TICKETS, Routes.PROFILE)
                } == true
                val showAdminBar = isAdminUser && !isCustomerDestination

                if (showAdminBar) {
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.Home, contentDescription = null) },
                        label = { Text(stringResource(id = R.string.nav_admin_dashboard)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.ADMIN_DASHBOARD } == true,
                        onClick = {
                            navController.navigate(Routes.ADMIN_DASHBOARD) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.Done, contentDescription = null) },
                        label = { Text(stringResource(id = R.string.nav_check_in)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.CHECK_IN } == true,
                        onClick = {
                            navController.navigate(Routes.CHECK_IN) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.AutoMirrored.Filled.List, contentDescription = null) },
                        label = { Text(stringResource(R.string.nav_admin_invoices)) },
                        selected = currentDestination?.hierarchy?.any {
                            it.route == Routes.ADMIN_INVOICES || it.route == Routes.ADMIN_INVOICE_DETAIL
                        } == true,
                        onClick = {
                            navController.navigate(Routes.ADMIN_INVOICES) {
                                popUpTo(navController.graph.findStartDestination().id) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.Settings, contentDescription = null) },
                        label = { Text(stringResource(R.string.nav_admin_statistics)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.ADMIN_STATISTICS } == true,
                        onClick = {
                            navController.navigate(Routes.ADMIN_STATISTICS) {
                                popUpTo(navController.graph.findStartDestination().id) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.AutoMirrored.Filled.List, contentDescription = null) },
                        label = { Text(stringResource(R.string.nav_admin_management)) },
                        selected = currentDestination?.hierarchy?.any {
                            it.route == Routes.ADMIN_MANAGEMENT ||
                                it.route == Routes.ADMIN_SHOWTIMES ||
                                it.route == Routes.ADMIN_SHOWTIME_DETAIL ||
                                it.route == Routes.ADMIN_MOVIES ||
                                it.route == Routes.ADMIN_MOVIE_DETAIL ||
                                it.route == Routes.ADMIN_COMBOS ||
                                it.route == Routes.ADMIN_VOUCHERS ||
                                it.route == Routes.ADMIN_REFUNDS
                        } == true,
                        onClick = {
                            navController.navigate(Routes.ADMIN_MANAGEMENT) {
                                popUpTo(navController.graph.findStartDestination().id) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                } else {
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.Home, contentDescription = null) },
                        label = { Text(stringResource(id = R.string.nav_home)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.HOME } == true,
                        onClick = {
                            navController.navigate(Routes.HOME) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.ShoppingCart, contentDescription = null) },
                        label = { Text(stringResource(id = R.string.nav_tickets)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.MY_TICKETS || it.route == Routes.TICKETS } == true,
                        onClick = {
                            navController.navigate(Routes.MY_TICKETS) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                    NavigationBarItem(
                        icon = { Icon(Icons.Filled.Person, contentDescription = null) },
                        label = { Text(stringResource(id = R.string.nav_profile)) },
                        selected = currentDestination?.hierarchy?.any { it.route == Routes.PROFILE } == true,
                        onClick = {
                            navController.navigate(Routes.PROFILE) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                }
            }
        }
    ) { innerPadding ->
        val effectiveStart = rememberSaveable {
            initialRoute ?: if (isAdminUser) Routes.ADMIN_DASHBOARD else Routes.HOME
        }

        NavHost(
            navController = navController,
            startDestination = effectiveStart,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable(Routes.ADMIN_DASHBOARD) {
                    AdminDashboardScreen(
                        navController = navController,
                        onNavigateToScanner = {
                            navController.navigate(Routes.CHECK_IN) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        },
                        onNavigateToShowtimeDetail = { showtimeId, showDate ->
                            navController.navigate(Routes.adminShowtimeDetail(showtimeId, showDate))
                        },
                        onLogout = handleAdminLogout
                    )
                }
                composable(Routes.CHECK_IN) {
                    AdminQrScannerScreen(
                        navController = navController,
                        adminName = adminDisplayName,
                        onBack = {
                            if (!navController.popBackStack()) {
                                navController.navigate(Routes.ADMIN_DASHBOARD) {
                                    popUpTo(navController.graph.findStartDestination().id) {
                                        saveState = true
                                    }
                                    launchSingleTop = true
                                    restoreState = true
                                }
                            }
                        },
                        onLogout = handleAdminLogout
                    )
                }
                composable(Routes.ADMIN_INVOICES) {
                    InvoiceListScreen(
                        adminName = adminDisplayName,
                        onLogout = handleAdminLogout,
                        onInvoiceClick = { invoiceId ->
                            navController.navigate(Routes.adminInvoiceDetail(invoiceId))
                        }
                    )
                }
                composable(
                    route = Routes.ADMIN_INVOICE_DETAIL,
                    arguments = listOf(navArgument("invoiceId") { type = NavType.StringType })
                ) {
                    InvoiceDetailScreen(onBack = { navController.popBackStack() })
                }
                composable(Routes.ADMIN_STATISTICS) {
                    StatisticsScreen(
                        adminName = adminDisplayName,
                        onLogout = handleAdminLogout
                    )
                }
                composable(Routes.ADMIN_MANAGEMENT) {
                    AdminManagementScreen(
                        adminName = adminDisplayName,
                        onLogout = handleAdminLogout,
                        onShowtimesClick = { navController.navigate(Routes.ADMIN_SHOWTIMES) },
                        onMoviesClick = { navController.navigate(Routes.ADMIN_MOVIES) },
                        onCombosClick = { navController.navigate(Routes.ADMIN_COMBOS) },
                        onVouchersClick = { navController.navigate(Routes.ADMIN_VOUCHERS) },
                        onRefundsClick = { navController.navigate(Routes.ADMIN_REFUNDS) }
                    )
                }
                composable(Routes.ADMIN_COMBOS) {
                    AdminComboScreen(onBack = { navController.popBackStack() })
                }
                composable(Routes.ADMIN_VOUCHERS) {
                    AdminVoucherScreen(onBack = { navController.popBackStack() })
                }
                composable(Routes.ADMIN_REFUNDS) {
                    AdminRefundScreen(onBack = { navController.popBackStack() })
                }
                composable(Routes.ADMIN_SHOWTIMES) {
                    AdminShowtimeListScreen(
                        onBack = { navController.popBackStack() },
                        onShowtimeClick = { id, date ->
                            navController.navigate(Routes.adminShowtimeDetail(id, date))
                        }
                    )
                }
                composable(
                    route = Routes.ADMIN_SHOWTIME_DETAIL,
                    arguments = listOf(
                        navArgument("showtimeId") { type = NavType.StringType },
                        navArgument("showDate") { type = NavType.StringType }
                    )
                ) {
                    AdminShowtimeDetailScreen(onBack = { navController.popBackStack() })
                }
                composable(Routes.ADMIN_MOVIES) {
                    AdminMovieListScreen(
                        onBack = { navController.popBackStack() },
                        onMovieClick = { id ->
                            navController.navigate(Routes.adminMovieDetail(id))
                        }
                    )
                }
            composable(
                route = Routes.ADMIN_MOVIE_DETAIL,
                arguments = listOf(navArgument("movieId") { type = NavType.StringType })
            ) {
                AdminMovieDetailScreen(onBack = { navController.popBackStack() })
            }

            composable(Routes.HOME) {
                HomeScreen(onMovieClick = { movieId ->
                    rootNavController.navigate(Routes.movieDetail(movieId))
                })
            }
            composable(Routes.MY_TICKETS) {
                MyTicketsScreen(
                    onNavigateToHome = {
                        navController.navigate(Routes.HOME) {
                            popUpTo(navController.graph.findStartDestination().id) {
                                saveState = true
                            }
                            launchSingleTop = true
                            restoreState = true
                        }
                    },
                    onTicketDetail = { invoiceId ->
                        rootNavController.navigate(Routes.ticketDetail(invoiceId))
                    }
                )
            }
            composable(Routes.TICKETS) {
                MyTicketsScreen(
                    onNavigateToHome = {
                        navController.navigate(Routes.HOME) {
                            popUpTo(navController.graph.findStartDestination().id) {
                                saveState = true
                            }
                            launchSingleTop = true
                            restoreState = true
                        }
                    },
                    onTicketDetail = { invoiceId ->
                        rootNavController.navigate(Routes.ticketDetail(invoiceId))
                    }
                )
            }
            composable(Routes.PROFILE) {
                ProfileScreen(
                    isAdmin = isAdminUser,
                    onNavigateToAdmin = if (isAdminUser) {
                        {
                            navController.navigate(Routes.ADMIN_DASHBOARD) {
                                popUpTo(Routes.HOME) {
                                    inclusive = true
                                }
                                launchSingleTop = true
                            }
                        }
                    } else null,
                    onLogout = {
                        rootNavController.navigate(Routes.AUTH) {
                            popUpTo(rootNavController.graph.id) { inclusive = true }
                            launchSingleTop = true
                        }
                    }
                )
            }
        }
    }
}
