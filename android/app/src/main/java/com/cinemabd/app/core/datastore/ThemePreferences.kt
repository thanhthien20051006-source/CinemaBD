package com.cinemabd.app.core.datastore

import android.content.Context
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ThemePreferences @Inject constructor(
    @ApplicationContext private val context: Context
) {
    private val DARK_THEME_KEY = booleanPreferencesKey("is_dark_theme")

    val isDarkThemeFlow: Flow<Boolean> = context.dataStore.data
        .map { preferences ->
            preferences[DARK_THEME_KEY] ?: true
        }

    suspend fun setDarkTheme(isDark: Boolean) {
        context.dataStore.edit { preferences ->
            preferences[DARK_THEME_KEY] = isDark
        }
    }
}
