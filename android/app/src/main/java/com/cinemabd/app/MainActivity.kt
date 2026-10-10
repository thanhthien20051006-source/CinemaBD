package com.cinemabd.app

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import androidx.navigation.compose.rememberNavController
import com.cinemabd.app.core.network.AuthEvent
import com.cinemabd.app.navigation.CinemaBdNavHost
import com.cinemabd.app.navigation.Routes
import com.cinemabd.app.ui.theme.CinemaBDTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    private val viewModel: MainViewModel by viewModels()
    private var navController: NavHostController? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            val isLoading by viewModel.isLoading.collectAsStateWithLifecycle()
            val startDestination by viewModel.startDestination.collectAsStateWithLifecycle()
            val isDarkTheme by viewModel.isDarkTheme.collectAsStateWithLifecycle()
            val nc = rememberNavController().also { navController = it }

            LaunchedEffect(Unit) {
                viewModel.authEventBus.events.collect { event ->
                    when (event) {
                        is AuthEvent.SessionExpired -> {
                            Toast.makeText(
                                this@MainActivity,
                                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                                Toast.LENGTH_LONG
                            ).show()
                            navController?.navigate(Routes.AUTH) {
                                popUpTo(0) { inclusive = true }
                            }
                        }
                    }
                }
            }

            CinemaBDTheme(darkTheme = isDarkTheme) {
                if (isLoading || startDestination == null) {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                } else {
                    CinemaBdNavHost(
                        navController = nc,
                        startDestination = startDestination!!
                    )
                }
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        navController?.handleDeepLink(intent)
    }
}
