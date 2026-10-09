# CinemaBD Android

Đồ án môn Phát triển ứng dụng di động. App khách hàng cho hệ thống đặt vé CinemaBD.

## Công nghệ
Kotlin, Jetpack Compose (Material 3), MVVM + Repository, Hilt,
Retrofit + OkHttp + Kotlin Serialization, Navigation Compose,
DataStore (JWT), Room (cache phim), Coil, SignalR client.

## Backend
- API: ../CinemaBD.Api (ASP.NET Core 8, JWT). Không sửa backend trừ khi được yêu cầu.
- Tài liệu API: ../docs/ANDROID_API_NOTES.md (đọc trước khi viết code gọi API).
- Base URL cấu hình qua BuildConfig (emulator: http://10.0.2.2:PORT).
- Tham chiếu giao diện: ../CinemaBD.Web/Views và wwwroot/css/cinema-theme.css.
- Một số endpoint không bọc ApiResponse: xử lý riêng trong Repository.

## Quy ước
- Package: com.cinemabd.app; chia theo feature (auth, movies, booking, ticket, profile).
- Mỗi màn hình có: Screen (Composable), ViewModel (StateFlow), UiState.
- Gọi API chỉ qua Repository, không gọi trong Composable.
- Luôn xử lý 3 trạng thái: loading, success, error.
- Chuỗi hiển thị để trong strings.xml (tiếng Việt).
- Sau mỗi màn hình: chạy ./gradlew assembleDebug, sửa lỗi rồi mới báo xong.
- Commit sau mỗi màn hình hoàn chỉnh.