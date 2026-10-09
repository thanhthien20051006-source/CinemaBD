package com.cinemabd.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import com.cinemabd.app.navigation.CinemaBdNavHost
import com.cinemabd.app.ui.theme.CinemaBDTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            CinemaBDTheme {
                CinemaBdNavHost()
            }
        }
    }
}
