# CinemaBD API notes cho Android

> **Phạm vi:** tài liệu này mô tả các endpoint dành cho khách hàng trong `CinemaBD.Api/Controllers`, không bao gồm các controller dưới `Controllers/Admin` hoặc các action quản trị trong `ReviewsController`.
>
> **Nguồn kiểm tra:** `CinemaBD.Api/Program.cs`, controller công khai, `Contracts`, middleware, `CinemaBD.Web/Hubs/SeatHub.cs`, và `CinemaBD.Web/Services/CinemaApiClient.cs`.

## 1. Base URL, port và CORS

| Môi trường | Base URL | Nguồn cấu hình |
|---|---|---|
| Chạy API cục bộ bằng profile `CinemaBD.Api` | `https://localhost:7067` | `CinemaBD.Api/Properties/launchSettings.json` |
| HTTP cục bộ | `http://localhost:5067` | `CinemaBD.Api/Properties/launchSettings.json` |
| API trong Docker, từ máy host | `http://localhost:5188` | ánh xạ `5188:8080` trong `docker-compose.yml` |
| API trong mạng Docker, từ service web | `http://api:8080` | `ApiSettings__BaseUrl` trong `docker-compose.yml` |

- Web hiện đặt `ApiSettings:BaseUrl` là `https://localhost:7067/` trong `CinemaBD.Web/appsettings.json`.
- `CinemaApiClient` dùng `http://localhost:5188/` nếu cấu hình thiếu, hoặc nếu base URL là `localhost:5001` hay `127.0.0.1:5001`.
- API gọi `UseHttpsRedirection()`. Android nên dùng URL HTTPS khi triển khai thực tế. Khi thử nghiệm với `localhost`, **localhost của điện thoại/emulator không phải máy chạy API**. Cần dùng IP LAN của máy chủ, alias emulator thích hợp, hoặc tunnel phát triển.
- Swagger được bật tại `/swagger`.

### CORS

Hiện API **không cấu hình CORS**: không có `AddCors`, `UseCors` hoặc `[EnableCors]`. `AllowedHosts: "*"` không phải cấu hình CORS.

CORS là chính sách của trình duyệt nên ứng dụng Android native không bị chặn bởi nó. Tuy vậy, nếu sau này Android dùng WebView hoặc API được gọi từ SPA chạy ở origin khác, backend phải bổ sung CORS trước.

## 2. Envelope phản hồi và cách xử lý lỗi

Hầu hết endpoint trả envelope chung `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Lấy danh sách phim thành công",
  "data": {}
}
```

Khai báo gốc là:

```csharp
public record ApiResponse<T>(bool Success, string Message, T? Data);
```

MVC tuần tự hóa các thuộc tính thành camel case, vì vậy Android nên đọc `success`, `message`, `data`.

### Quy tắc Android nên áp dụng

1. Luôn kiểm tra **cả HTTP status lẫn `success`**. Một số lỗi nghiệp vụ trả HTTP `200` với `success: false`.
2. Khi response là envelope, hiển thị `message` phù hợp và chỉ đọc `data` nếu kiểu dữ liệu cho phép.
3. HTTP `400`, `404`, và `500` thường có envelope lỗi với `success: false` và `data: null`.
4. `401` do JWT thiếu, sai hoặc hết hạn được middleware xác thực mặc định trả về, nên không được bảo đảm có envelope `ApiResponse`. Android phải coi `401` là yêu cầu đăng nhập lại.
5. Framework có thể trả `400 ValidationProblemDetails` trước khi action chạy nếu JSON sai kiểu hoặc thiếu giá trị không-nullable. Response này cũng không theo `ApiResponse`.
6. `GET /api/payments/vnpay-ipn` là ngoại lệ: trả object `{ "rspCode", "message" }`, không bọc `ApiResponse`.

### Lỗi từ middleware toàn cục

`ExceptionHandlingMiddleware` xử lý lỗi chưa được controller bắt:

| Tình huống | HTTP | Nội dung |
|---|---:|---|
| `ArgumentException` | 400 | `ApiResponse(false, ex.Message, null)` |
| `InvalidOperationException` | 400 | `ApiResponse(false, ex.Message, null)` |
| Lỗi khác | 500 | `ApiResponse(false, "Đã xảy ra lỗi máy chủ.", null)` |

> Lưu ý: middleware gọi trực tiếp `JsonSerializer.Serialize` mà không đặt web options. Tùy cấu hình serializer runtime, key của **lỗi do middleware** có thể giữ PascalCase (`Success`, `Message`, `Data`). Android nên parse key không phân biệt hoa thường hoặc cấu hình JSON parser hỗ trợ cả hai cách viết.

## 3. Đăng ký, đăng nhập và JWT

### Lấy token

Gọi một trong hai endpoint công khai sau:

- `POST /api/auth/login`
- `POST /api/auth/register`

Cả hai trả `data.token`. Lưu token an toàn trong Android, ví dụ Jetpack Security `EncryptedSharedPreferences` hoặc Android Keystore. Không ghi token vào log, analytics hay thông báo lỗi.

Ví dụ response đăng nhập:

```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": {
    "userId": "KH...",
    "username": "nguyenvana",
    "fullName": "Nguyễn Văn A",
    "token": "eyJ..."
  }
}
```

Gửi token cho các route có JWT:

```http
Authorization: Bearer <data.token>
```

JWT được ký HMAC SHA-256, kiểm tra issuer, audience, chữ ký và hạn dùng. Token hiện hết hạn sau **12 giờ UTC**. Claim gồm `sub`, `unique_name`, `full_name`, role và `account_type`. API dùng `sub` để xác định khách hàng hiện tại.

Không có refresh-token endpoint. Khi nhận `401`, Android nên xóa token cũ, đưa người dùng về đăng nhập, và không tự gửi lại mật khẩu.

### Request mẫu

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "nguyenvana",
  "password": "mat-khau"
}
```

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "Nguyễn Văn A",
  "username": "nguyenvana",
  "password": "mat-khau",
  "email": "a@example.com",
  "phoneNumber": "0900000000"
}
```

## 4. Danh mục endpoint khách hàng

**Quy ước trong bảng:**

- `Envelope<T>` là `ApiResponse<T>` với `data` có kiểu `T`.
- `JWT` nghĩa là bắt buộc `Authorization: Bearer <token>`.
- Trường đánh dấu `?` là nullable hoặc tùy chọn.
- Các endpoint thành công hiện đều trả HTTP `200`, không dùng `201 Created`.

### Xác thực

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| POST | `/api/auth/login` | JSON `{ username, password }` | `AuthResponse` gồm `userId`, `username`, `fullName`, `token` | Không |
| POST | `/api/auth/register` | JSON `{ fullName, username, password, email?, phoneNumber? }` | `AuthResponse` | Không |

Lỗi thiếu username/password, tài khoản không tồn tại, sai mật khẩu, dữ liệu đăng ký thiếu hoặc username trùng đi qua middleware thành HTTP `400` envelope. Sai mật khẩu **không** trả HTTP `401` trong implementation hiện tại.

### Phim và suất chiếu

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| GET | `/api/movies` | Không có | `MovieResponse[]` | Không |
| GET | `/api/movies/{id}` | Path `id` | `MovieResponse` | Không |
| GET | `/api/movies/{id}/showtimes` | Path `id`; query `date?` theo ISO date, ví dụ `2026-04-04` | `ShowtimeResponse[]` | Không |

`MovieResponse`:

```text
id, title, genre?, durationMinutes, director?, cast?, country?,
ageRestriction?, description?, posterUrl?, trailerUrl?, releaseDate?, endDate?, status?
```

`ShowtimeResponse`:

```text
id, showDate, startTime, roomId, roomName, ticketPrice, totalSeats,
availableSeats, status, heldSeats, soldSeats, checkedInSeats, canEdit, canDelete
```

- Phim không tồn tại tại `GET /api/movies/{id}` trả HTTP `404` envelope.
- Suất chiếu của phim không tồn tại không bị báo lỗi riêng. API có thể trả danh sách rỗng.
- Với danh sách suất chiếu, nếu không truyền `date`, service chọn ngày hiện tại theo giờ Việt Nam.

### Combo

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| GET | `/api/combos` | Không có | `Combo[]` | Không |
| GET | `/api/combos/{id}` | Path `id` | `Combo` | Không |

`Combo` là object từ domain có các trường `id`, `name`, `price`, `description?`, `imageUrl?`.

Combo bị đánh dấu tên bắt đầu `[Ngưng bán]` không xuất hiện trong danh sách và truy vấn chi tiết trả HTTP `404` envelope.

### Bài viết và sự kiện

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| GET | `/api/articles` | Query `page=1`, `pageSize=9` | `Paged<Article>` | Không |
| GET | `/api/articles/{id}` | Path `id:int` | `Article` | Không |
| GET | `/api/events` | Query `page=1`, `pageSize=9` | `Paged<Event>` | Không |

`page` được ép tối thiểu là `1`; `pageSize` bị giới hạn từ `1` đến `50`.

```text
Paged<T> = { items: T[], page, pageSize, total, totalPages }
Article  = { id, title, summary?, content?, imageUrl?, publishedAt }
Event    = { id, title, description?, imageUrl?, startDate?, endDate? }
```

Bài viết không tồn tại trả HTTP `404` envelope. API chỉ trả sự kiện còn hiệu lực, nghĩa là `endDate` null hoặc không sớm hơn ngày hiện tại.

### Sơ đồ ghế, giữ ghế, thanh toán và hóa đơn

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| GET | `/api/bookings/showtimes/{showtimeId}/seats` | Path `showtimeId` | `SeatResponse[]` | Không |
| POST | `/api/bookings/hold-seats` | JSON `SeatHoldRequest` | `SeatHoldResult` | Có |
| POST | `/api/bookings/release-seats` | JSON `SeatHoldRequest` | `SeatHoldResult` | Có |
| POST | `/api/bookings/checkout` | JSON `CheckoutRequest`; header `X-Return-Url?` | `CheckoutResponse` | Có |
| POST | `/api/bookings/refund-request` | JSON `RefundRequestBody` | `RefundRequestResult` | Có |
| GET | `/api/bookings/invoice/{txnRef}` | Path `txnRef` | `InvoiceResponse` | Có |

```json
// SeatHoldRequest
{
  "showtimeId": "SC20260404120000",
  "seats": ["A1", "A2"]
}
```

```json
// CheckoutRequest
{
  "showtimeId": "SC20260404120000",
  "seats": ["A1", "A2"],
  "combos": "CB01:1,CB02:2",
  "totalAmount": 180000
}
```

`combos` là chuỗi phân tách bằng dấu phẩy. Mỗi combo theo dạng `maCombo:soLuong`. Implementation hiện cũng đọc `voucher:maVoucher` và `points:soDiem` trong chuỗi này khi checkout.

```json
// RefundRequestBody
{
  "transactionRef": "TXN...",
  "ticketId": "VE...",
  "reason": "Lý do yêu cầu hủy vé"
}
```

`ticketId` có thể null để yêu cầu hủy theo giao dịch/hóa đơn.

```text
SeatResponse = { id, row, column, seatType?, isBooked, status, price }
SeatHoldResult = {
  success, message, showtimeId, heldSeats: string[],
  unavailableSeats: string[], expiresAt?
}
CheckoutResponse = { transactionRef, paymentUrl, totalAmount }
RefundRequestResult = { success, message, data? }
InvoiceResponse = {
  transactionRef, paymentId, paymentStatus, totalAmount,
  movieId?, movieTitle?, moviePosterUrl?, showDate?, startTime?, roomName?,
  seats: string[], tickets: InvoiceTicketResponse[], combos: string[]
}
InvoiceTicketResponse = { ticketId, seatId, price, status?, isCheckedIn, checkedInAt? }
```

Lưu ý nghiệp vụ quan trọng:

- `GET .../seats` trả `404` envelope nếu không tìm thấy suất chiếu.
- Ghế được giữ trong **5 phút**. Hết thời gian, pending ticket được hết hạn khi dịch vụ xử lý lượt giữ/checkout tiếp theo.
- `hold-seats`, `release-seats` và `refund-request` có thể trả HTTP `200` nhưng envelope ngoài và object `data` đều có `success: false`. Đọc `message` thay vì chỉ dựa trên HTTP status.
- `checkout` kiểm tra giá, ghế, combo, voucher và điểm ở server. Trường request `totalAmount` đã được bind nhưng implementation hiện không dùng để tính số tiền cuối cùng. Android phải hiển thị `data.totalAmount` từ response.
- Có thể gửi `X-Return-Url` khi app muốn chỉ định return URL được dùng để tạo URL VNPay. Chỉ gửi URL do ứng dụng kiểm soát.
- Chỉ khách sở hữu hóa đơn đã thanh toán mới lấy được hóa đơn. Trường hợp không tồn tại, chưa thanh toán, không thuộc người gọi, hoặc thiếu dữ liệu hóa đơn đều trả `404` envelope.

### Callback thanh toán

| Method | Đường dẫn | Request | Response | JWT |
|---|---|---|---|---|
| GET | `/api/payments/vnpay-return` | Toàn bộ query VNPAY | `Envelope<PaymentCallbackResponse>` | Không |
| GET | `/api/payments/vnpay-ipn` | Toàn bộ query VNPAY | `{ rspCode, message }` | Không |
| GET | `/api/payments/demo-confirm` | Query `txnRef`, `gateway=MOMO` | `Envelope<PaymentCallbackResponse>` | Không, Development-only |

```text
PaymentCallbackResponse = {
  success, signatureValid, transactionRef?, responseCode?, message
}
```

- `vnpay-return` là callback/return endpoint. Các thất bại xác thực chữ ký, trạng thái thanh toán hoặc giao dịch vẫn có thể là HTTP `200` với `success: false`.
- `vnpay-ipn` dành cho VNPay server-to-server, không phải endpoint Android gọi trực tiếp.
- `demo-confirm` chỉ hoạt động khi ASP.NET environment là `Development`; ở môi trường khác trả HTTP `404` không bọc envelope.
- Sau khi mở `paymentUrl`, Android cần xử lý deep link/app link hoặc web return flow của cổng thanh toán. Không coi việc mở URL là thanh toán thành công. Xác nhận bằng callback flow và truy vấn invoice được bảo vệ sau đó.

### Đánh giá phim

| Method | Đường dẫn | Request | Response `data` | JWT |
|---|---|---|---|---|
| GET | `/api/reviews/movie/{movieId}` | Path `movieId` | `ReviewResponse[]` | Không |
| GET | `/api/reviews/movie/{movieId}/eligibility` | Path `movieId` | `ReviewResponse` | Có |
| POST | `/api/reviews` | JSON `CreateReviewRequest` | `ReviewResponse` | Có |

```json
{
  "movieId": "VN019",
  "content": "Phim hay, âm thanh tốt.",
  "rating": 5
}
```

```text
ReviewResponse = {
  id, movieId, customerId, content, rating, isHidden,
  canReview, reviewRuleMessage?, createdAt, movieTitle?, customerName?
}
```

- Review bị ẩn không xuất hiện trong danh sách công khai.
- Eligibility không đạt điều kiện trả HTTP `200` cùng `success: false`, không phải `403`.
- Nội dung đánh giá trống trả HTTP `400` envelope.
- Lỗi nghiệp vụ khi tạo đánh giá được controller bắt và trả HTTP `200` cùng `success: false`.
- Các action `/api/reviews/admin...` là quản trị và không dành cho Android client khách hàng.

### Hồ sơ, điểm thưởng và lịch sử mua

Tất cả endpoint phần này cần JWT.

| Method | Đường dẫn | Request | Response `data` |
|---|---|---|---|
| GET | `/api/account/profile` | Không có | `CustomerProfileResponse` |
| PUT | `/api/account/profile` | JSON `UpdateCustomerProfileRequest` | `CustomerProfileResponse` |
| GET | `/api/account/loyalty` | Không có | `LoyaltyPoint` hoặc fallback |
| POST | `/api/account/loyalty/preview-redeem` | JSON `LoyaltyRedeemRequest` | `LoyaltyRedeemResult` |
| GET | `/api/account/history` | Không có | `CustomerHistoryResponse[]` |

```json
// UpdateCustomerProfileRequest
{
  "fullName": "Nguyễn Văn A",
  "email": "a@example.com",
  "phoneNumber": "0900000000",
  "birthDate": "2000-01-31T00:00:00"
}
```

```json
// LoyaltyRedeemRequest
{
  "points": 30,
  "subtotal": 180000
}
```

```text
CustomerProfileResponse = {
  userId, username, fullName, email?, phoneNumber?, birthDate?, totalSpent
}
LoyaltyPoint = {
  id, customerId, customerName?, rewardPoints, earnedPoints, usedPoints,
  balance, pointsToMoney, moneyPerPoint, pointValue
}
LoyaltyRedeemResult = {
  success, message, usedPoints, discountAmount, remainingPoints
}
CustomerHistoryResponse = {
  invoiceId, movieTitle?, showDate?, startTime?, totalAmount, status?,
  paymentDate, ticketCount, checkedInCount, seatIds
}
```

- Profile không tồn tại trả `404` envelope.
- `fullName` rỗng khi cập nhật trả `400` envelope.
- Không có lịch sử mua hợp lệ trả list rỗng.
- Preview đổi điểm là tính thử. Android phải đưa kết quả vào `combos` của checkout dưới dạng `points:<số_điểm>` nếu người dùng chọn áp dụng.
- Các lỗi điều kiện đổi điểm thường là HTTP `200` với `success: false`.
- Nếu hệ thống thiếu bảng tích điểm, endpoint loyalty trả fallback số dư bằng `0`. `GET /loyalty` trả outer `success: true`, còn preview trả outer `success: false` cùng fallback số 0.

## 5. Luồng tích hợp Android đề xuất

### Đặt vé

1. Lấy phim qua `GET /api/movies` và chi tiết/lịch chiếu theo nhu cầu.
2. Lấy sơ đồ ghế với `GET /api/bookings/showtimes/{showtimeId}/seats`.
3. Đăng nhập trước khi giữ ghế.
4. Gọi `POST /api/bookings/hold-seats` ngay khi xác nhận chọn ghế. Kiểm tra cả envelope ngoài và `data.success`.
5. Lấy combo bằng `GET /api/combos`. Nếu có, preview điểm qua endpoint loyalty và ghép chuỗi `combos`.
6. Gọi `POST /api/bookings/checkout`, dùng `paymentUrl` và `totalAmount` do server trả về.
7. Mở `paymentUrl` qua Custom Tab hoặc WebView có kiểm soát deep link. Hoàn tất return flow của VNPay.
8. Sau thanh toán, lấy `GET /api/bookings/invoice/{txnRef}` kèm JWT để hiển thị vé. Chỉ coi vé hợp lệ khi API trả invoice.

Không nên chỉ dựa vào trạng thái cục bộ của ghế hoặc response từ SignalR để kết luận ghế đã thuộc về khách. REST API là nguồn dữ liệu có thẩm quyền.

### Hủy vé và đánh giá

- Chỉ gửi refund request sau khi người dùng xác nhận. API từ chối vé đã check-in, vé gần giờ chiếu dưới hai giờ, hoặc request đang chờ duyệt.
- Trước khi cho đăng review, gọi eligibility. Nếu `success: false`, hiển thị `message` thay vì cố gửi review.

## 6. Web hiện gọi API như thế nào

### `CinemaApiClient`

Web đăng ký `CinemaApiClient` bằng `HttpClient` và gọi API qua route tương đối, ví dụ `api/movies`, `api/bookings/checkout`, `api/account/profile`.

- Base URL lấy từ `ApiSettings:BaseUrl` với fallback `http://localhost:5188/` trong `CinemaBD.Web/Program.cs`.
- Client deserializes phần lớn response thành `ApiResponse<T>` rồi lấy `data`.
- Token không được tự động chuyển tiếp. Các method bảo vệ nhận token rõ ràng và đặt header:

```http
Authorization: Bearer <token>
```

- Web lưu token khách trong ASP.NET session với key `UserToken` sau login/register.
- Một số method web chỉ trả `null` hoặc list rỗng khi HTTP lỗi. Android không nên sao chép hành vi này. Hãy parse error body để giữ `message` hữu ích cho người dùng.

### `SeatHub` và SignalR

`SeatHub` **không nằm trong CinemaBD.Api**. Đây là hub do CinemaBD.Web host tại:

```text
/hubs/seats
```

Web browser kết nối cùng origin và dùng session cookie. Hub tự lấy `UserToken` từ ASP.NET session rồi gọi REST API để giữ/nhả ghế.

| Hub method | Hành vi |
|---|---|
| `JoinShowtime(showtimeId)` | Vào group `showtime:{showtimeId}` |
| `HoldSeat(showtimeId, seatId, seatCode)` | Lấy token từ Web session, gọi REST `hold-seats`; thành công mới broadcast `SeatHeld` |
| `ReleaseSeat(showtimeId, seatId)` | Gọi REST `release-seats` nếu có token, rồi broadcast `SeatReleased` |
| `ConfirmSeats(showtimeId, seatIds)` | Chỉ broadcast `SeatsConfirmed`, không gọi REST |

Điểm quan trọng cho Android:

- Hub không có JWT bearer configuration hoặc `accessTokenFactory` cho kết nối SignalR.
- Token không thể chỉ truyền như mobile bearer token để hub hiện tại xác thực. Hub phụ thuộc vào cookie session của **CinemaBD.Web**.
- Vì vậy Android nên gọi REST trực tiếp cho trạng thái ghế. Không nên coi endpoint hub web hiện tại là giao thức realtime chính thức cho mobile.
- Nếu cần realtime native, backend cần thiết kế riêng một hub API có JWT access-token handling, initial seat snapshot, rejoin group sau reconnect, và contract event ổn định.

## 7. Tham chiếu mã nguồn chính

| Nội dung | Tệp |
|---|---|
| Pipeline API, JWT, Swagger, HTTPS | `CinemaBD.Api/Program.cs` |
| Envelope chung | `CinemaBD.Api/Contracts/Common/ApiResponse.cs` |
| Error middleware | `CinemaBD.Api/Middleware/ExceptionHandlingMiddleware.cs` |
| JWT 12 giờ và claims | `CinemaBD.Infrastructure/Security/JwtTokenService.cs` |
| Auth customer | `CinemaBD.Api/Controllers/AuthController.cs` |
| Luồng booking, invoice, refund | `CinemaBD.Api/Controllers/BookingsController.cs` và `CinemaBD.Infrastructure/Services/BookingService.cs` |
| Client REST của web | `CinemaBD.Web/Services/CinemaApiClient.cs` |
| Đồng bộ ghế SignalR phía Web | `CinemaBD.Web/Hubs/SeatHub.cs` |
| Port local | `CinemaBD.Api/Properties/launchSettings.json` |
| Port Docker | `docker-compose.yml` |
