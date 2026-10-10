package com.cinemabd.app.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.feature.auth.AuthScreen
import com.cinemabd.app.feature.hello.HelloScreen

object Destinations {
    const val HELLO = "hello"
    const val AUTH = "auth"
}

@Composable
fun CinemaBdNavHost(
    navController: NavHostController = rememberNavController(),
    startDestination: String = Destinations.AUTH
) {
    NavHost(
        navController = navController,
        startDestination = startDestination
    ) {
        composable(Destinations.AUTH) {
            AuthScreen(
                onAuthSuccess = {
                    navController.navigate(Destinations.HELLO) {
                        popUpTo(Destinations.AUTH) { inclusive = true }
                    }
                }
            )
        }
        composable(Destinations.HELLO) {
            HelloScreen()
        }
    }
}
