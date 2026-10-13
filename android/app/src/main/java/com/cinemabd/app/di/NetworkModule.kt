package com.cinemabd.app.di

import com.cinemabd.app.BuildConfig
import com.cinemabd.app.core.network.AuthInterceptor
import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {

    @Provides
    @Singleton
    fun provideJson(): Json {
        return Json {
            ignoreUnknownKeys = true
            isLenient = true
            explicitNulls = false
        }
    }

    @Provides
    @Singleton
    fun provideOkHttpClient(
        authInterceptor: AuthInterceptor
    ): OkHttpClient {
        val builder = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)

        if (BuildConfig.DEBUG) {
            val logging = HttpLoggingInterceptor { message ->
                val sanitized = message
                    .replace(Regex("\"password\"\\s*:\\s*\"[^\"]*\""), "\"password\":\"***\"")
                    .replace(Regex("\"token\"\\s*:\\s*\"[^\"]*\""), "\"token\":\"***\"")
                android.util.Log.d("OkHttp", sanitized)
            }.apply {
                level = HttpLoggingInterceptor.Level.BODY
                redactHeader("Authorization")
                redactHeader("Cookie")
            }
            builder.addInterceptor(logging)
        }

        return builder.build()
    }

    @Provides
    @Singleton
    fun provideRetrofit(
        okHttpClient: OkHttpClient,
        json: Json
    ): Retrofit {
        val contentType = "application/json".toMediaType()
        return Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(json.asConverterFactory(contentType))
            .build()
    }

    @Provides
    @Singleton
    fun provideAuthApi(retrofit: Retrofit): com.cinemabd.app.feature.auth.AuthApi {
        return retrofit.create(com.cinemabd.app.feature.auth.AuthApi::class.java)
    }

    @Provides
    @Singleton
    fun provideMoviesApi(retrofit: Retrofit): com.cinemabd.app.core.network.MoviesApi {
        return retrofit.create(com.cinemabd.app.core.network.MoviesApi::class.java)
    }

    @Provides
    @Singleton
    fun provideReviewsApi(retrofit: Retrofit): com.cinemabd.app.core.network.ReviewsApi {
        return retrofit.create(com.cinemabd.app.core.network.ReviewsApi::class.java)
    }

    @Provides
    @Singleton
    fun provideBookingsApi(retrofit: Retrofit): com.cinemabd.app.core.network.BookingsApi {
        return retrofit.create(com.cinemabd.app.core.network.BookingsApi::class.java)
    }

    @Provides
    @Singleton
    fun provideCombosApi(retrofit: Retrofit): com.cinemabd.app.core.network.CombosApi {
        return retrofit.create(com.cinemabd.app.core.network.CombosApi::class.java)
    }

    @Provides
    @Singleton
    fun providePaymentsApi(retrofit: Retrofit): com.cinemabd.app.core.network.PaymentsApi {
        return retrofit.create(com.cinemabd.app.core.network.PaymentsApi::class.java)
    }

    @Provides
    @Singleton
    fun provideAccountApi(retrofit: Retrofit): com.cinemabd.app.core.network.AccountApi {
        return retrofit.create(com.cinemabd.app.core.network.AccountApi::class.java)
    }

    @Provides
    @Singleton
    fun provideAdminApi(retrofit: Retrofit): com.cinemabd.app.feature.admin.AdminApi {
        return retrofit.create(com.cinemabd.app.feature.admin.AdminApi::class.java)
    }
}
