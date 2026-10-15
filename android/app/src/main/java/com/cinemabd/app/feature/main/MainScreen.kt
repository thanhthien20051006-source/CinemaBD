package com.cinemabd.app.feature.main

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Done
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.NavHostController
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.R
import com.cinemabd.app.feature.admin.CheckInScreen
import com.cinemabd.app.feature.home.HomeScreen
import com.cinemabd.app.feature.profile.ProfileScreen
import com.cinemabd.app.feature.tickets.TicketsScreen
import com.cinemabd.app.navigation.Routes

@Composable
fun MainScreen(
    rootNavController: NavHostController,
    viewModel: MainScreenViewModel = hiltViewModel()
) {
    val navController = rememberNavController()
    val isAdmin by viewModel.isAdmin.collectAsStateWithLifecycle()

    if (isAdmin == null) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }
    val isAdminUser = isAdmin == true

    Scaffold(
        bottomBar = {
            NavigationBar {
                val navBackStackEntry by navController.currentBackStackEntryAsState()
                val currentDestination = navBackStackEntry?.destination

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
                    selected = currentDestination?.hierarchy?.any { it.route == Routes.TICKETS } == true,
                    onClick = {
                        navController.navigate(Routes.TICKETS) {
                            popUpTo(navController.graph.findStartDestination().id) {
                                saveState = true
                            }
                            launchSingleTop = true
                            restoreState = true
                        }
                    }
                )
                if (isAdminUser) {
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
                }
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
    ) { innerPadding ->
        NavHost(
            navController = navController,
            startDestination = if (isAdminUser) Routes.CHECK_IN else Routes.HOME,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable(Routes.HOME) {
                HomeScreen(onMovieClick = { movieId ->
                    rootNavController.navigate(Routes.movieDetail(movieId))
                })
            }
            composable(Routes.TICKETS) {
                TicketsScreen(
                    onTicketClick = { invoiceId ->
                        rootNavController.navigate(Routes.ticketDetail(invoiceId))
                    }
                )
            }
            if (isAdminUser) {
                composable(Routes.CHECK_IN) {
                    CheckInScreen()
                }
            }
            composable(Routes.PROFILE) {
                ProfileScreen(
                    onLogout = {
                        rootNavController.navigate(Routes.AUTH) {
                            popUpTo(0) { inclusive = true }
                        }
                    }
                )
            }
        }
    }
}
