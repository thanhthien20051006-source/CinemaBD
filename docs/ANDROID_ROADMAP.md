# Lộ trình hoàn thiện ứng dụng Android CinemaBD

> **Phạm vi:** đánh giá tĩnh mã nguồn Android hiện có, đối chiếu các API khách hàng/quản trị với API backend, và đề xuất thứ tự hoàn thiện ứng dụng đa vai trò Khách hàng–Quản trị viên. Tài liệu này không thay đổi hay cam kết thay đổi backend. Trạng thái bên dưới phản ánh các Retrofit interface/DTO tìm thấy trong checkout hiện tại; không phải kết quả kiểm thử runtime.

## 1. Tóm tắt điều hành

Ứng dụng đã có nền tảng MVVM tương đối đầy đủ: Jetpack Compose, Navigation Compose, Hilt, Retrofit/OkHttp, Kotlin Serialization, Coroutines/Flow (thể hiện qua các hàm `suspend`, `StateFlow`), DataStore, Room, Coil, cùng các luồng đặt vé khách hàng và nhiều màn quản trị. Vì vậy, **chưa có căn cứ để đề xuất thay framework hay thêm một thư viện DI/network/database mới**.

Các khoảng trống ưu tiên theo API:

1. Bổ sung API nhả ghế và hoàn chỉnh kiểm thử vòng đời giữ/nhả ghế.
2. Bổ sung thao tác tạo/xóa phim và tạo/sửa/xóa/sinh suất chiếu; hiện Android có danh sách/chi tiết/cập nhật phim và danh sách/hủy suất chiếu.
3. Làm rõ “vé của tôi”: Android có lịch sử hóa đơn và chi tiết hóa đơn; backend notes không thể hiện endpoint danh sách vé riêng.
4. Làm rõ nghiệp vụ voucher khách hàng: backend hiện biểu diễn voucher trong chuỗi `combos` khi checkout; không có endpoint xác thực voucher dành cho khách trong danh mục API hiện tại. Không nên gọi API quản trị voucher từ tài khoản khách.
5. Rà soát các giao diện/DTO phụ trợ: review chỉ đọc; preview đổi điểm, refund request, Google login và một số endpoint đã có ở backend nhưng chưa khai báo trong Android.

## 2. Kiểm kê công nghệ và kiến trúc hiện tại

### 2.1. Thư viện trong `android/app/build.gradle.kts`

| Nhu cầu | Hiện trạng | Đánh giá / đề xuất |
|---|---|---|
| UI | Jetpack Compose, Material 3, Activity Compose, Lifecycle Compose | Đã có; tiếp tục dùng.
| Navigation | Navigation Compose | Đã có; có nested booking graph và deep link kết quả thanh toán.
| MVVM | Lifecycle ViewModel Compose; ViewModels sử dụng `StateFlow` | Đã có mẫu triển khai. Tiếp tục giữ `Screen → ViewModel → Repository → Retrofit`.
| DI | Hilt Android, Hilt Navigation Compose, KSP compiler | Đã có; không cần thêm Koin/Dagger khác.
| HTTP | Retrofit 2, OkHttp logging interceptor, Kotlin Serialization converter | Đã có. Tập trung thống nhất envelope/error handling và kiểm thử DTO.
| Coroutines/Flow | Dùng `suspend`, `viewModelScope`, `StateFlow` trong mã nguồn | Có dấu hiệu dùng tích cực; chưa thấy dependency khai báo riêng `kotlinx-coroutines` trong app-level build file. Xác nhận dependency transitive/catalog và khai báo trực tiếp nếu module sử dụng API Coroutines mà không có dependency trực tiếp.
| Local persistence | DataStore Preferences; Room runtime/ktx/compiler | Đã có. Room hiện có `TicketEntity`/`TicketDao`, không phải cache phim như phần mô tả trong `android/CLAUDE.md`. DB đang dùng destructive migration fallback; cần cân nhắc migration có bảo toàn dữ liệu trước khi mở rộng sử dụng.
| Ảnh | Coil Compose | Đã có; sử dụng cho poster/ảnh nội dung.
| Thanh toán/QR | Chrome Custom Tabs; ZXing core + embedded | Đã có dependency; xác minh hành vi deep link và quyền camera/QR theo luồng app thực tế.
| Realtime ghế | Không thấy SignalR client/dependency trong Gradle và nguồn đã kiểm tra | Chỉ bổ sung nếu sản phẩm yêu cầu cập nhật ghế realtime. Hiện REST API là nguồn trạng thái có thẩm quyền; SignalR là nâng cấp tùy chọn, không chặn MVP.
| Kiểm thử | JUnit, AndroidX Test, Espresso, Compose UI test | Có dependency; cần lập/hoàn thiện unit, contract và UI test tương ứng. Không kết luận coverage chỉ dựa vào dependencies.

### 2.2. Các lớp và chức năng tìm thấy

- **Entrypoint/DI:** `CinemaBdApplication`, `MainActivity`, `di/NetworkModule.kt`, `di/DatabaseModule.kt`.
- **Network/models:** `core/network/` (Movies, Bookings, Combos, Payments, Reviews, Account), `feature/auth/AuthApi.kt`, các interface admin trong `feature/admin/`.
- **Repositories:** `data/repository/` gồm Movies, Bookings, Combos, Payments, Reviews, Profile, Tickets; Auth và Admin có lớp riêng.
- **Trạng thái:** ViewModels và `UiState` dùng `StateFlow`; `BookingSharedViewModel` chia sẻ lựa chọn xuyên qua booking graph.
- **Phiên:** DataStore lưu token/theme; `SessionManager`/`MainViewModel` xác định role và điều hướng.
- **Local DB:** Room hiện lưu vé; `TicketsRepository` có đường cache/fallback.
- **Navigation/screens:** `CinemaBdNavHost.kt` chứa auth, khám phá phim, chi tiết, chọn suất/ghế/combo, checkout, kết quả thanh toán và chi tiết vé. `MainScreen.kt` tách bottom navigation khách/admin.

### 2.3. Cấu hình cần hoàn thiện trước phát hành

- `android/app/build.gradle.kts` đặt debug base URL `http://10.0.2.2:5067/` (địa chỉ emulator tới host local), release là `https://cinemabd.example.com/` với chú thích thay sau. Thay placeholder bằng cấu hình môi trường triển khai trước khi phát hành; xác minh release chỉ dùng HTTPS.
- `docs/ANDROID_API_NOTES.md` nêu lưu JWT an toàn, parse cả HTTP status lẫn `success` trong envelope; 401 phải dẫn tới xóa session/đăng nhập lại. Kiểm tra redaction logging và không log token/mật khẩu.
- Nếu tiếp tục dùng Room như nơi lưu dữ liệu bền vững, lên kế hoạch migration thay cho destructive fallback; quyết định rõ cache vé/phim nào cần có và quy tắc hết hạn/đồng bộ.
- Android hướng dẫn trong `android/CLAUDE.md` mô tả Room cache phim và SignalR; hiện trạng mã nguồn quan sát được chưa khớp hai điểm này. Cập nhật hướng dẫn khi nhóm quyết định kiến trúc thực tế.

## 3. Đối chiếu API Android với backend

**Nhãn:** `Có` = có Retrofit method phù hợp; `Một phần` = có một phần thao tác/DTO; `Thiếu` = không tìm thấy method; `Cần xác nhận` = endpoint hoặc ý nghĩa nghiệp vụ không đủ rõ để kết luận. Endpoint lấy từ `docs/NIST_1_IDENTIFY.md`, `docs/ANDROID_API_NOTES.md`, `docs/ANDROID_ADMIN_API_NOTES.md`, đối chiếu interface Android và ghi chú backend.

### 3.1. Khách hàng

| Chức năng | Endpoint backend | Android hiện có | Trạng thái và điểm cần làm |
|---|---|---|---|
| Đăng nhập khách | `POST /api/auth/login` | `AuthApi.login(LoginRequest) → AuthResponse` | **Có.** Bao gồm token và điều hướng theo session/role.
| Đăng ký | `POST /api/auth/register` | `AuthApi.register(RegisterRequest) → AuthResponse` | **Có.**
| Google login | `POST /api/auth/google` | Không thấy method/DTO Google trong `AuthApi` | **Thiếu** nếu vẫn nằm trong phạm vi sản phẩm; xác nhận client ID và luồng Google trước khi lên sprint.
| Phim | `GET /api/movies`, `GET /api/movies/{id}` | `MoviesApi` với `MovieResponse` | **Có.**
| Suất chiếu | `GET /api/movies/{id}/showtimes?date=` | `MoviesApi.getShowtimes` với `ShowtimeResponse` | **Có.**
| Danh sách/chi tiết combo | `GET /api/combos`, `GET /api/combos/{id}` | `CombosApi`, `ComboResponse` | **Có.**
| Sơ đồ ghế | `GET /api/bookings/showtimes/{showtimeId}/seats` | `BookingsApi.getSeats → SeatResponse[]` | **Có.**
| Giữ ghế | `POST /api/bookings/hold-seats` | `BookingsApi.holdSeats → SeatHoldResult` | **Có.** Kiểm tra cả envelope `success` và `data.success` theo API notes.
| Nhả ghế | `POST /api/bookings/release-seats` | Không thấy trong `BookingsApi` | **Thiếu.** Bổ sung method và gọi khi hủy/chuyển bước/thoát chọn ghế phù hợp; kiểm tra timeout và xử lý lỗi. Không khẳng định cần thêm backend endpoint — endpoint đã có.
| Checkout/đặt vé | `POST /api/bookings/checkout` | `BookingsApi.checkout(CheckoutRequest) → CheckoutResponse` | **Có.** Server tính tổng cuối; hiển thị `totalAmount` từ response, không tin giá client gửi.
| Thanh toán VNPay return/demo | `GET /api/payments/vnpay-return`, `GET /api/payments/demo-confirm` | `PaymentsApi` có cả hai | **Có một phần.** `demo-confirm` chỉ Development. Không thấy `vnpay-ipn`, nhưng đây là server-to-server callback, không phải API Android cần gọi.
| Lịch sử của tôi | `GET /api/account/history` | `AccountApi.getHistory → CustomerHistoryResponse[]` | **Có** cho lịch sử giao dịch/hóa đơn.
| Chi tiết vé/hóa đơn | `GET /api/bookings/invoice/{txnRef}` | Có trong `BookingsApi.getInvoice` và `AccountApi.getInvoiceDetail` | **Có, nhưng trùng khai báo.** Chọn một điểm truy cập/repository chuẩn để tránh lệch logic.
| Danh sách từng vé | Không thấy endpoint riêng trong API inventory; có history và invoice | Tickets UI/repository lấy lịch sử/cache vé; `InvoiceResponse` chứa danh sách `tickets` | **Cần xác nhận phạm vi.** Xác nhận sản phẩm muốn danh sách hóa đơn hay từng vé; không tự suy ra API riêng.
| Hồ sơ | `GET/PUT /api/account/profile` | `AccountApi` + profile DTO | **Có.**
| Điểm thưởng | `GET /api/account/loyalty` | `AccountApi.getLoyalty` | **Có một phần.**
| Preview đổi điểm | `POST /api/account/loyalty/preview-redeem` | Không thấy method/DTO | **Thiếu.** Cần bổ sung nếu cho khách áp dụng điểm từ UI; backend notes yêu cầu truyền chọn điểm vào `combos` checkout.
| Đánh giá phim | `GET /api/reviews/movie/{movieId}` | `ReviewsApi.getReviewsByMovie` | **Có một phần.** Danh sách có; thiếu `GET .../eligibility` và `POST /api/reviews` để hoàn chỉnh tạo đánh giá.
| Yêu cầu hoàn/hủy vé | `POST /api/bookings/refund-request` | Không thấy ở customer `BookingsApi` | **Thiếu** nếu luồng tự phục vụ khách cần hỗ trợ. Không nhầm với admin approve/reject refund đã có.
| Bài viết/sự kiện | `GET /api/articles`, `GET /api/events` | Không thấy Retrofit service trong danh sách interface kiểm tra | **Thiếu/ngoài phạm vi ưu tiên.** Chỉ thêm nếu các màn nội dung nằm trong yêu cầu sản phẩm.

### 3.2. Quản trị viên

| Chức năng | Endpoint backend | Android hiện có | Trạng thái và điểm cần làm |
|---|---|---|---|
| Đăng nhập admin | `POST /api/admin/auth/login` | `AuthApi.adminLogin → AdminAuthResponse` | **Có.**
| Hóa đơn và chi tiết | `GET /api/admin/invoices`, `GET /api/admin/invoices/{id}` | `AdminApi` + invoice DTOs | **Có.**
| Thống kê doanh thu | `GET /api/admin/statistics/revenue?year=` | `AdminApi.getRevenueStatistics` | **Có.**
| Check-in QR | `POST /api/admin/invoices/check-in` | `AdminApi.checkIn` + `CheckInRequest/Result` | **Có.**
| Quản lý phim | `GET /api/admin/movies`, `GET /api/admin/movies/{id}`, `PUT /api/admin/movies/{id}` | `AdminMovieApi`; màn danh sách/chi tiết | **Một phần.** Thiếu `POST` tạo và `DELETE`; xác minh form hiện tại chỉ sửa trạng thái hay hỗ trợ toàn bộ fields của `AdminMovieUpsertRequest`.
| Quản lý suất chiếu | `GET /api/admin/showtimes?date=`, `POST /api/admin/showtimes/{id}/cancel` | `AdminShowtimeApi`; màn danh sách/chi tiết/hủy | **Một phần.** Thiếu `POST /api/admin/showtimes`, `PUT/DELETE /{id}`, `POST /generate`, `POST /expire-passed`; method GET chưa expose query `roomId` tùy chọn của backend.
| Quản lý combo | CRUD `/api/admin/combos` | `AdminComboApi` CRUD + models | **Có** theo interface.
| Quản lý voucher | CRUD `/api/admin/vouchers` | `AdminVoucherApi` CRUD + models | **Có** cho quản trị. Không sử dụng làm API khách hàng.
| Quản lý yêu cầu hoàn | `GET /api/admin/refunds`, approve/reject | `AdminRefundApi` + models | **Có** theo interface.
| Dashboard | `GET /api/admin/dashboard` | Không thấy trong Retrofit admin interface | **Thiếu** nếu cần màn dashboard riêng; hiện có trang thống kê doanh thu riêng.
| Rạp/phòng/ghế | `/api/admin/cinemas`, `/rooms`, `/seats` | Không thấy service tương ứng trong admin interface đã kiểm kê | **Thiếu/ngoài sprint ưu tiên**; cần lên roadmap nếu admin phải vận hành cấu hình cơ sở vật chất trên app.
| Khách hàng/nhân viên/vai trò/thể loại/nội dung/điểm | Các route `/api/admin/customers`, `/employees`, `/roles`, `/genres`, `/articles`, `/events`, `/loyalty-points` | Không thấy Retrofit service tương ứng trong các interface admin hiện có | **Thiếu/ngoài phạm vi cốt lõi**; phân rã thành module riêng sau khi chốt quyền và yêu cầu nghiệp vụ.

### 3.3. Quy ước tích hợp bắt buộc

- Với mọi envelope: kiểm tra HTTP status và `success`; các thao tác giữ/nhả ghế còn có `data.success`. Không coi HTTP 200 là luôn thành công.
- Với 401: xóa token/session, phát sự kiện đăng xuất và chuyển về đăng nhập; không retry bằng mật khẩu lưu trữ.
- Checkout dùng response server làm nguồn tiền cuối cùng; mở `paymentUrl` không chứng minh thanh toán thành công. Chỉ hiển thị vé khi truy vấn hóa đơn thành công.
- Chỉ gửi return URL do ứng dụng kiểm soát; không dùng URL tùy ý do người dùng nhập.
- Tất cả API quản trị phải gửi JWT role phù hợp; kiểm tra role trong navigation không thay thế authorization backend.
- Không ghi token, mật khẩu, QR nhạy cảm hoặc dữ liệu thanh toán vào log.

## 4. Danh sách màn hình

### Khách hàng — hiện có theo navigation/source

1. Đăng nhập/đăng ký.
2. Trang chủ/danh sách phim; chi tiết phim.
3. Chọn suất chiếu, chọn ghế, chọn combo, checkout.
4. Kết quả thanh toán qua deep link `cinemabd://payment-result?txnRef=...`.
5. Danh sách vé/lịch sử và chi tiết hóa đơn-vé.
6. Hồ sơ tài khoản.

**Màn/luồng nên hoàn thiện:** xử lý lỗi và retry/empty state nhất quán; nhả ghế khi rời booking; áp dụng/preview điểm; làm rõ voucher khách hàng; đánh giá phim; yêu cầu hoàn vé; Google login nếu vẫn hỗ trợ. Thêm danh sách bài viết/sự kiện chỉ khi thuộc phạm vi sản phẩm ưu tiên.

### Quản trị viên — hiện có theo navigation/source

1. Check-in QR.
2. Danh sách và chi tiết hóa đơn.
3. Thống kê doanh thu.
4. Trung tâm quản trị.
5. Danh sách/chi tiết phim.
6. Danh sách/chi tiết suất chiếu.
7. Quản lý combo, voucher, yêu cầu hoàn tiền.

**Màn/luồng nên hoàn thiện:** form tạo/xóa phim; tạo/sửa/xóa và sinh suất chiếu; bộ lọc rạp/phòng/ngày; dashboard tổng quan nếu cần; sau đó mới mở rộng cấu hình rạp/phòng/ghế, khách hàng/nhân viên/role.

## 5. Đề xuất thư viện và kiến trúc MVVM

### 5.1. Có cần bổ sung dependency không?

| Đề xuất | Ưu tiên | Lý do / điều kiện |
|---|---|---|
| Coroutines (`kotlinx-coroutines-android` và test artifact) khai báo trực tiếp/version catalog | Kiểm tra trước, thêm nếu thiếu | Mã dùng `viewModelScope`, `StateFlow`, `suspend`; hiện app-level Gradle chưa khai báo Coroutines trực tiếp. Xác minh dependency transitive trước để tránh thêm trùng.
| MockWebServer | Nên thêm khi viết network contract tests | Cho phép kiểm tra HTTP method/path/header/body, serializer và error envelope cho Retrofit mà không gọi backend thật.
| Turbine | Tùy chọn | Thuận tiện kiểm thử `Flow`/`StateFlow`; không bắt buộc nếu JUnit hiện tại đủ.
| SignalR Android client | Tùy chọn, chưa cần cho MVP | Chỉ dùng khi yêu cầu cập nhật ghế realtime đã chốt; cần xác định hub hiện nằm ở `CinemaBD.Web`, không phải API host chính, và thiết kế fallback REST.
| Room migration test / schema export | Nên cân nhắc nếu mở rộng cache bền vững | Tránh mất dữ liệu bởi destructive migration; chỉ tăng phạm vi Room sau khi xác định dữ liệu cache cần giữ.

Không cần thêm Navigation, Hilt/Koin, Retrofit/OkHttp, Room, Coil hoặc Coroutines/Flow framework khác—các thành phần chính đã có mặt trong catalog/dependencies hoặc sử dụng ở mã nguồn.

### 5.2. Luồng kiến trúc đề nghị

```text
Compose Screen
  └─ collectAsStateWithLifecycle(UiState)
       └─ ViewModel (viewModelScope; state + user actions)
            └─ Repository (quy tắc tích hợp, ánh xạ lỗi/envelope, IO)
                 ├─ Retrofit API + DTO (request/response backend)
                 └─ Room/DataStore khi có yêu cầu cache/session cụ thể
```

Giữ API interface thuần khai báo HTTP; không gọi Retrofit trực tiếp trong Composable. Duy trì `UiState` cho Loading/Success/Empty/Error, tách DTO mạng khỏi UI model khi cần xử lý nhiều nguồn dữ liệu. Thống nhất cách đọc `ApiResponse`, lỗi HTTP/401 và lỗi không envelope. Tránh tạo thêm abstraction nếu repository hiện tại đã làm đúng vai trò.

## 6. Thứ tự triển khai theo Sprint

Thời lượng là gợi ý để lập kế hoạch nhóm, không phải cam kết estimate. Mỗi sprint nên hoàn tất API/DTO → repository → ViewModel/UiState → màn hình → kiểm thử tích hợp trước khi mở phạm vi kế tiếp.

### Sprint 0 — Chuẩn hóa nền tảng và xác nhận nghiệp vụ

- Chốt endpoint nguồn cho từng feature, đặc biệt: ticket list hay invoice history; voucher phía khách; Google login; có cần realtime ghế không.
- Kiểm tra version catalog/dependency graph cho Coroutines; chuẩn hóa cấu hình URL debug/staging/release qua build config an toàn.
- Rà soát DTO/envelope, 401/logout và redaction log; thống nhất test convention.
- **Tiêu chí hoàn tất:** quyết định nghiệp vụ được ghi lại; không còn release URL placeholder; có checklist contract cho luồng đăng nhập, booking, thanh toán và admin.

### Sprint 1 — Đăng nhập, phiên và khám phá phim

- Hoàn thiện test/validation cho login, register, admin login, role routing và 401; thêm Google login chỉ sau khi xác nhận phạm vi.
- Kiểm tra phim/danh sách/chi tiết/suất chiếu, ảnh poster, loading/empty/error/retry.
- **Màn hình:** auth, home, movie detail, chọn suất.
- **Tiêu chí hoàn tất:** user/admin vào đúng nhánh điều hướng; token không xuất hiện trong log; dữ liệu backend khớp DTO; hỗ trợ lỗi mạng và dữ liệu rỗng.

### Sprint 2 — Đặt ghế, checkout và thanh toán

- Bổ sung `release-seats` vào `BookingsApi`, DTO/repository và xử lý các điểm rời booking.
- Kiểm tra luồng sơ đồ ghế → giữ → combo → checkout → mở payment URL → deep link → xác minh invoice; bổ sung contract test cho envelope và lỗi nghiệp vụ.
- Làm rõ/triển khai preview điểm và voucher theo API hiện hữu; không tạo API khách mới trong tài liệu nếu backend chưa có.
- **Màn hình:** chọn ghế, combo, checkout, kết quả thanh toán.
- **Tiêu chí hoàn tất:** giữ/nhả ghế đúng user; thời hạn hold/lỗi ghế bận được hiển thị; chỉ dùng tổng tiền từ server; chỉ hiển thị vé sau invoice đã thanh toán.

### Sprint 3 — Vé, tài khoản và chăm sóc khách hàng

- Hợp nhất điểm gọi invoice trùng giữa `BookingsApi`/`AccountApi`.
- Chốt semantics Tickets screen: lịch sử hóa đơn so với từng ticket; sửa repository/DTO hoặc đề nghị backend endpoint riêng chỉ khi có yêu cầu được xác nhận.
- Bổ sung preview loyalty, review eligibility/create, refund request; làm rõ voucher dành cho khách và xác minh server contract.
- **Màn hình:** vé/lịch sử, chi tiết vé, hồ sơ, đánh giá, yêu cầu hoàn.
- **Tiêu chí hoàn tất:** thao tác riêng tư cần JWT; lỗi quyền/không có dữ liệu có thông báo; kiểm thử ownership bằng user khác nhau ở môi trường test.

### Sprint 4 — Hoàn thiện vận hành admin

- Bổ sung DTO/API/repository cho tạo và xóa phim; đầy đủ CRUD/generate/cancel/expire suất chiếu và filter room/date theo backend.
- Mở rộng form quản trị và xác nhận quy tắc xóa/cancel/sinh hàng loạt trước khi cho thao tác; giữ xác nhận người dùng với hành động phá hủy hoặc ảnh hưởng nhiều suất.
- **Màn hình:** form phim, form suất chiếu, lịch/bộ lọc, tạo hàng loạt; giữ check-in, hóa đơn, thống kê, combo, voucher và refund hiện tại.
- **Tiêu chí hoàn tất:** mọi thao tác Admin gửi token Admin; quyền/validation lỗi được trình bày; kiểm thử không gửi mutation trùng khi người dùng bấm liên tục.

### Sprint 5 — Mở rộng, ổn định và phát hành

- Ưu tiên theo yêu cầu sản phẩm: dashboard; rạp/phòng/ghế; khách hàng/nhân viên/role; bài viết/sự kiện.
- Xem xét realtime ghế bằng SignalR client chỉ khi backend hub/host và hành vi fallback đã rõ; REST vẫn quyết định trạng thái cuối.
- Hoàn thiện test unit ViewModel/Repository, Retrofit contract tests bằng MockWebServer, Compose UI smoke tests, Room migration tests nếu DB cần giữ dữ liệu.
- **Tiêu chí hoàn tất:** debug/staging/release host đúng; HTTPS ở release; đường dẫn thanh toán/deep link hoạt động; quy trình login–booking–invoice và admin check-in được kiểm tra end-to-end; không còn crash blocker và không lộ secret/token.

## 7. Danh sách kiểm chứng tổng thể

- API contract: đúng method/path/query/header/body, serializer camelCase, nullable/default DTO, envelope và response đặc thù.
- Trạng thái UI: Loading, Success, Empty, Error, retry; hành vi khi offline và khi nhận 401.
- Khách hàng: đăng ký/đăng nhập → phim → suất → giữ/nhả ghế → checkout → thanh toán → tải invoice/vé.
- Admin: đăng nhập role Admin → CRUD được hỗ trợ → check-in QR → hóa đơn/thống kê/refund.
- Dữ liệu: Room migration/cache không xóa dữ liệu ngoài dự kiến; DataStore logout xóa session.
- Cấu hình: URL production HTTPS; không chứa secret thật; logging không lộ dữ liệu xác thực.
- Kiểm thử Android sau khi triển khai từng sprint theo hướng dẫn `android/CLAUDE.md` (`./gradlew assembleDebug`) và các unit/instrumented tests tương ứng. Chưa chạy build hay test trong lần phân tích tài liệu này.

## 8. Tài liệu tham chiếu

- `android/CLAUDE.md`
- `android/app/build.gradle.kts`, `android/gradle/libs.versions.toml`
- Android Retrofit interfaces/models/repositories/navigation dưới `android/app/src/main/java/com/cinemabd/app/`
- `docs/NIST_1_IDENTIFY.md`
- `docs/ANDROID_API_NOTES.md`
- `docs/ANDROID_ADMIN_API_NOTES.md`
- API host: `CinemaBD.Api/Controllers/`, `CinemaBD.Api/Contracts/`
