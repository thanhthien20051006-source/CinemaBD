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
import com.cinemabd.app.core.session.SessionEvent
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
            val isAdmin by viewModel.isAdmin.collectAsStateWithLifecycle()
            val isDarkTheme by viewModel.isDarkTheme.collectAsStateWithLifecycle()
            val nc = rememberNavController().also { navController = it }

            LaunchedEffect(Unit) {
                viewModel.sessionManager.events.collect { event ->
                    when (event) {
                        is SessionEvent.SessionExpired -> {
                            Toast.makeText(
                                this@MainActivity,
                                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                                Toast.LENGTH_LONG
                            ).show()
                            navController?.let { controller ->
                                val graphId = runCatching { controller.graph.id }.getOrNull()
                                controller.navigate(Routes.AUTH) {
                                    if (graphId != null) {
                                        popUpTo(graphId) { inclusive = true }
                                    } else {
                                        popUpTo(0) { inclusive = true }
                                    }
                                    launchSingleTop = true
                                }
                            }
                        }
                    }
                }
            }

            CinemaBDTheme(darkTheme = isDarkTheme) {
                if (isLoading || startDestination == null || isAdmin == null) {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                } else {
                    CinemaBdNavHost(
                        navController = nc,
                        startDestination = startDestination!!,
                        isAdmin = isAdmin == true
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
