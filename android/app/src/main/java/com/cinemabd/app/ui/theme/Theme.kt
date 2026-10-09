package com.cinemabd.app.ui.theme

import android.app.Activity
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

// CinemaBD Theme Colors (from cinema-theme.css)
val CinemaDark = Color(0xFF0B1020)
val CinemaPanel = Color(0xFF111827)
val CinemaRed = Color(0xFFE50914)
val CinemaOrange = Color(0xFFFF6B16)
val CinemaYellow = Color(0xFFFACC15)
val CinemaGreen = Color(0xFF16A34A)
val CinemaText = Color(0xFFF8FAFC)
val CinemaMuted = Color(0xFF94A3B8)

private val DarkColorScheme = darkColorScheme(
    primary = CinemaRed,
    secondary = CinemaOrange,
    tertiary = CinemaYellow,
    background = CinemaDark,
    surface = CinemaPanel,
    onPrimary = Color.White,
    onSecondary = Color.White,
    onTertiary = CinemaDark,
    onBackground = CinemaText,
    onSurface = CinemaText,
    error = CinemaRed,
    onError = Color.White
)

@Composable
fun CinemaBDTheme(
    content: @Composable () -> Unit
) {
    val colorScheme = DarkColorScheme
    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = colorScheme.background.toArgb()
            window.navigationBarColor = colorScheme.background.toArgb()
            WindowCompat.getInsetsController(window, view).apply {
                isAppearanceLightStatusBars = false
                isAppearanceLightNavigationBars = false
            }
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        content = content
    )
}
