package com.cinemabd.app.feature.main

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.R
import com.cinemabd.app.feature.home.HomeScreen

object MainDestinations {
    const val HOME = "home"
    const val TICKETS = "tickets"
    const val PROFILE = "profile"
}

@Composable
fun MainScreen() {
    val navController = rememberNavController()

    Scaffold(
        bottomBar = {
            NavigationBar {
                val navBackStackEntry by navController.currentBackStackEntryAsState()
                val currentDestination = navBackStackEntry?.destination

                NavigationBarItem(
                    icon = { Icon(Icons.Filled.Home, contentDescription = null) },
                    label = { Text(stringResource(id = R.string.nav_home)) },
                    selected = currentDestination?.hierarchy?.any { it.route == MainDestinations.HOME } == true,
                    onClick = {
                        navController.navigate(MainDestinations.HOME) {
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
                    selected = currentDestination?.hierarchy?.any { it.route == MainDestinations.TICKETS } == true,
                    onClick = {
                        navController.navigate(MainDestinations.TICKETS) {
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
                    selected = currentDestination?.hierarchy?.any { it.route == MainDestinations.PROFILE } == true,
                    onClick = {
                        navController.navigate(MainDestinations.PROFILE) {
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
            startDestination = MainDestinations.HOME,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable(MainDestinations.HOME) {
                HomeScreen(onMovieClick = { movieId ->
                    // Handle movie click later (e.g. navigate to details)
                })
            }
            composable(MainDestinations.TICKETS) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(stringResource(id = R.string.nav_tickets))
                }
            }
            composable(MainDestinations.PROFILE) {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(stringResource(id = R.string.nav_profile))
                }
            }
        }
    }
}
