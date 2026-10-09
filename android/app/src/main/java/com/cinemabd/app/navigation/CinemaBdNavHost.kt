package com.cinemabd.app.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.feature.hello.HelloScreen

object Destinations {
    const val HELLO = "hello"
}

@Composable
fun CinemaBdNavHost(
    navController: NavHostController = rememberNavController(),
    startDestination: String = Destinations.HELLO
) {
    NavHost(
        navController = navController,
        startDestination = startDestination
    ) {
        composable(Destinations.HELLO) {
            HelloScreen()
        }
    }
}
