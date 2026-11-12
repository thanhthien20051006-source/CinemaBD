# Báo cáo Khảo sát Hiện trạng IAM (NIST CSF 1.1 Baseline) — CinemaBD

> Tài liệu này được tạo trong **Phase 0** theo hướng dẫn tại `docs/nist-iam-prompt.md`.  
> Mục tiêu: Đánh giá hiện trạng kiểm soát truy cập và quản lý định danh (IAM - trọng tâm nhóm **PR.AC** và các nhóm liên quan PR.DS, PR.PT, PR.IP, DE.AE, DE.CM, RS, RC, ID) trong toàn bộ hệ thống CinemaBD (gồm `CinemaBD.Api`, `CinemaBD.Web`, `CinemaBD.Infrastructure`, `CinemaBD.Domain` và client `android/`), kế thừa và bổ sung từ `docs/NIST_1_IDENTIFY.md`.

---

## 1. Sơ đồ Luồng Xác thực Hiện tại (Authentication Flows)

### 1.1. Luồng Khách hàng Đăng ký và Đăng nhập (Password)

```text
[Khách hàng / App]
       |
       | 1. POST /api/auth/register (FullName, Username, Password, Email, Phone) [RateLimit: auth]
       v
[AuthController.Register]
       |
       | 2. Gọi AuthService.RegisterAsync(...)
       v
[AuthService]
       |-- 3. Kiểm tra trùng lặp: TKhoan đã tồn tại?
       |-- 4. Băm mật khẩu: Md5PasswordHasher.Hash(password) -> "IDENTITY:" + PBKDF2(HMAC-SHA256, 100k iter)
       |-- 5. Lưu Customer (MaKH = "KH" + yyyyMMddHHmmssfff) vào SQL Server (bảng KHACHHANG)
       |-- 6. Gọi JwtTokenService.CreateToken(account)
       v
[Client nhận Token] (Role = "Customer", Expire = 12h)

--------------------------------------------------------------------------------------------------

[Khách hàng / App]
       |
       | 1. POST /api/auth/login (Username, Password) [RateLimit: auth]
       v
[AuthController.Login]
       |
       | 2. Gọi AuthService.LoginAsync(...)
       v
[AuthService]
       |-- 3. Tra cứu DB: Customers.FirstOrDefault(TKhoan == username)
       |      [!] Nếu không thấy -> Trả lỗi: "Tài khoản không tồn tại." (Lộ diện tài khoản / Enumeration)
       |-- 4. Xác minh mật khẩu: Md5PasswordHasher.Verify(password, user.MatKhau)
       |      [!] Nếu sai -> Trả lỗi: "Sai mật khẩu." (Lộ diện tài khoản / Enumeration)
       |-- 5. Cơ chế Rehash: Nếu NeedsRehash(user.MatKhau) (thiếu prefix "IDENTITY:")
       |      -> Hash lại bằng PBKDF2 và lưu ngay vào DB
       |-- 6. Gọi JwtTokenService.CreateToken(account)
       v
[Client nhận Token] (sub = MaKH, role = "Customer", exp = 12h)
```

### 1.2. Luồng Khách hàng Đăng nhập bằng Google

```text
[Khách hàng / App]
       |
       | 1. POST /api/auth/google (IdToken) [RateLimit: auth]
       v
[AuthController.GoogleLogin]
       |
       | 2. AuthService.GoogleLoginAsync(idToken)
       v
[AuthService]
       |-- 3. Gọi GoogleJsonWebSignature.ValidateAsync(idToken, Audience = GoogleAuth:ClientIds)
       |-- 4. Kiểm tra payload.EmailVerified == true
       |-- 5. Tra cứu DB: Customers.FirstOrDefault(Email == payload.Email)
       |      |-- Nếu đã có: Dùng tài khoản hiện tại (chưa kiểm tra tài khoản liên kết có mật khẩu yếu hay không)
       |      +-- Nếu chưa có: Tạo mới MaKH, mật khẩu ngẫu nhiên 32 bytes (băm PBKDF2), Username = Email
       |-- 6. Gọi JwtTokenService.CreateToken(account)
       v
[Client nhận Token] (sub = MaKH, role = "Customer", exp = 12h)
```

### 1.3. Luồng Khách hàng Đăng xuất và Thu hồi Token

```text
[Khách hàng / App]
       |
       | 1. POST /api/auth/logout [Authorize Bearer Token]
       v
[AuthController.Logout]
       |
       | 2. Đọc claim Jti và claim Exp từ JWT hiện tại
       | 3. Gọi IRevokedTokenService.RevokeAsync(jti, expiresAtUtc)
       v
[RevokedTokenService]
       |-- 4. Lưu bản ghi { Jti, ExpiresAtUtc } vào bảng RevokedTokens trong SQL Server
       v
[Client nhận 200 OK "Đã đăng xuất."]

Khi Client gửi lại Token cũ:
[API Request] -> JwtBearerHandler
       |
       |-- JwtBearerEvents.OnTokenValidated
       |-- Đọc jti từ token
       |-- Kiểm tra RevokedTokenService.IsRevokedAsync(jti)
       |-- Nếu tồn tại trong bảng RevokedTokens -> context.Fail("JWT has been revoked.") -> Trả về 401 Unauthorized
```

### 1.4. Luồng Quản trị viên (Admin) Đăng nhập

```text
[Admin Client / Web Admin / Android Admin]
       |
       | 1. POST /api/admin/auth/login (Username, Password) [RateLimit: auth]
       v
[AdminAuthController.Login]
       |
       | 2. AdminAuthApiService.LoginAsync(...)
       v
[AdminAuthApiService]
       |-- 3. Tra cứu DB: Admins.FirstOrDefault(Username == username && IsActive)
       |      [!] Nếu không thấy -> Trả lỗi: "Tài khoản admin không tồn tại." (Enumeration)
       |-- 4. Xác minh mật khẩu: Md5PasswordHasher.Verify(password, admin.Password)
       |      [!] Nếu sai -> Trả lỗi: "Sai mật khẩu admin." (Enumeration)
       |-- 5. Rehash: [LỖ HỔNG LOGIC]
       |      Chỉ rehash nếu string.Equals(admin.Password, password, StringComparison.Ordinal) (tức là plaintext).
       |      Nếu mật khẩu là MD5 thì KHÔNG ĐƯỢC nâng cấp lên "IDENTITY:" PBKDF2!
       |-- 6. Sinh token: JwtTokenService.CreateToken(UserAccount { Id = "admin:" + admin.AdminID, ... })
       |      Do Id bắt đầu bằng "admin:", claim Role được gán cứng là "Admin".
       v
[Admin nhận AdminAuthResponse] (Token có role "Admin", exp = 12h, role nghiệp vụ như Master/Manager không vào token)
[!] LƯU Ý: AdminAuthController KHÔNG có endpoint logout.
```

---

## 2. Cấu hình JWT (JSON Web Token)

Căn cứ: `CinemaBD.Api/Program.cs:99-143`, `CinemaBD.Infrastructure/Security/JwtTokenService.cs:1-50`.

| Tiêu chí | Cấu hình hiện tại | Đánh giá / Rủi ro theo NIST PR.AC / PR.DS |
|---|---|---|
| **Thuật toán ký** | HMAC-SHA256 (`SecurityAlgorithms.HmacSha256`) | Đạt chuẩn đối xứng hiện đại cho Web API nội bộ. |
| **Thời hạn Access Token** | **12 giờ** (`DateTime.UtcNow.AddHours(12)`) | **Rủi ro cao (PR.AC-1)**: Quá dài cho access token; nếu token bị lộ trên kênh truyền/client, kẻ tấn công có quyền truy cập đến nửa ngày. Cần rút ngắn xuống 10-15 phút và bổ sung refresh token. |
| **Kiểm tra Token** | `ValidateIssuer = true`<br>`ValidateAudience = true`<br>`ValidateLifetime = true`<br>`ValidateIssuerSigningKey = true`<br>`RequireExpirationTime = true` | Đạt chuẩn đầy đủ các thuộc tính xác thực JWT. |
| **ClockSkew** | `TimeSpan.Zero` | Tốt, không cho phép dung sai thời gian hết hạn. |
| **Độ dài khóa bí mật** | Kiểm tra fail-fast khi khởi động: `Encoding.UTF8.GetByteCount(jwtKey) >= 32` bytes (256 bits). Kiểm tra không bắt đầu bằng `"CHANGE_ME"`. | Đạt tiêu chuẩn tối thiểu HMAC-SHA256. |
| **Nơi lưu trữ khóa** | Đọc qua `builder.Configuration["Jwt:Key"]` (ưu tiên biến môi trường `JWT_KEY`, fallback `appsettings.json`). | Phù hợp nguyên tắc không lưu secret trong source code. |
| **Claims phát hành** | `sub` (`user.Id`), `jti` (GUID 32 ký tự hex), `unique_name` (`user.Username`), `full_name`, `role` (`"Admin"` hoặc `"Customer"`), `account_type` (`"admin"` hoặc `"customer"`). | **Thiếu (PR.AC-4)**: Chưa đưa danh mục quyền (permissions) hoặc mã vai trò nghiệp vụ cụ thể (`MaCV`) vào token; chỉ có 2 nhóm quyền thô: Admin và Customer. |
| **Xoay khóa (Key Rotation)** | Chỉ cấu hình 1 key duy nhất, không có tham số `kid` (Key ID), không hỗ trợ fallback sang key cũ khi xoay khóa. | **Chưa có (PR.AC-1, PR.DS)**: Xoay khóa sẽ làm vô hiệu hóa lập tức toàn bộ token đang lưu hành. |

---

## 3. Lưu trữ Mật khẩu và Cơ chế Băm

Căn cứ: `CinemaBD.Infrastructure/Security/Md5PasswordHasher.cs:1-47`, `AuthService.cs:39-47`, `AdminAuthApiService.cs:28-36`.

| Tiêu chí | Hiện trạng mã nguồn | Nhận xét / Khoảng cách |
|---|---|---|
| **Thuật toán băm mới** | ASP.NET Core Identity `PasswordHasher<UserAccount>` (chuẩn PBKDF2 với HMAC-SHA256, 100,000 iterations, 128-bit salt, 256-bit subkey). Giá trị lưu kèm tiền tố `"IDENTITY:"`. | Đạt chuẩn an toàn hiện đại của Microsoft cho .NET 8. |
| **Hỗ trợ ngược (Legacy Compatibility)** | Cho phép xác minh mật khẩu cũ định dạng: <br>1. MD5 hex 32 ký tự không salt (`HashMd5(plainTextPassword)`).<br>2. Plaintext (so sánh chuỗi trực tiếp `string.Equals(storedHash, plainTextPassword)`). | Tương thích ngược với CSDL cũ từ đồ án MVC 5, nhưng tiềm ẩn rủi ro cao nếu DB bị lộ (F-16 trong Identify). |
| **Rehash-on-login (Khách hàng)** | `AuthService.LoginAsync` kiểm tra `_passwordHasher.NeedsRehash(user.MatKhau)`. Nếu chuỗi hash trong DB không bắt đầu bằng `"IDENTITY:"`, tự động băm lại bằng PBKDF2 và lưu vào CSDL ngay khi đăng nhập đúng mật khẩu. | Đạt yêu cầu nâng cấp tự động không bắt người dùng reset mật khẩu hàng loạt. |
| **Rehash-on-login (Admin)** | `AdminAuthApiService.LoginAsync` chỉ kiểm tra: `if (string.Equals(admin.Password, password, StringComparison.Ordinal))` -> chỉ rehash nếu mật khẩu cũ là **plaintext**. | **LỖ HỔNG LOGIC**: Nếu tài khoản Admin đang lưu mật khẩu dạng MD5, sau khi đăng nhập thành công hệ thống **KHÔNG rehash** sang PBKDF2 mà vẫn giữ nguyên chuỗi MD5 trong DB! |
| **Chính sách mật khẩu (Complexity / Policy)** | Khi đăng ký (`AuthService.RegisterAsync`) chỉ kiểm tra `string.IsNullOrWhiteSpace(password)`. | **Thiếu (PR.AC-1)**: Chưa kiểm tra độ dài tối thiểu (ví dụ 8-12 ký tự), chưa có từ điển chặn mật khẩu phổ biến (`123456`, `password`...). |
| **Khóa tài khoản (Account Lockout)** | Không có đếm số lần đăng nhập sai, không có trường `AccessFailedCount` hay `LockoutEndUtc` trong entity `Customer` và `Admin`. | **Thiếu (PR.AC-7, DE.AE-2)**: Dễ bị tấn công vét cạn (brute-force) hoặc credential stuffing nếu vượt qua rate limit IP. |

### 3.1. Thống kê Số lượng Tài khoản theo Định dạng Băm Mật khẩu Hiện hữu

Kiểm tra trực tiếp từ `CinemaBD.sql` và `DatabaseInitializer.cs`:

| Nhóm tài khoản | Nguồn lưu trữ | Định dạng mật khẩu | Số lượng | Ghi chú an ninh |
|---|---|---|---|---|
| **Quản trị (Admin)** | `CinemaBD.sql` | **MD5 hex (32 ký tự không salt)** | **1** | Username `admin`, hash MD5 cũ. Cần nâng cấp tự động qua rehash-on-login. |
| **Quản trị (Admin)** | `DatabaseInitializer.cs` | **Plaintext** | **1** | Tài khoản demo fallback (`admin` / `admin123`). |
| **Quản trị (Admin)** | CSDL / Seed | **IDENTITY: (PBKDF2 chuẩn mới)** | **0** | Chưa có tài khoản admin nào được băm dạng chuẩn mới. |
| **Khách hàng (Customer)** | `CinemaBD.sql` | **MD5 hex (32 ký tự không salt)** | **3** | `KH001` (ttt), `KH1679797060` (ttt123), `KH20251118203033366` (ttt1). |
| **Khách hàng (Customer)** | `DatabaseInitializer.cs` | **Plaintext** | **3** | `KHDEMO01`, `KHDEMO02`, `KHDEMO03` (mật khẩu demo `123456`). |
| **Khách hàng (Customer)** | CSDL / Seed | **IDENTITY: (PBKDF2 chuẩn mới)** | **0** | Chưa có tài khoản khách hàng nào trong seed lưu dạng `IDENTITY:`. |

> **Tổng kết**: Hiện có **4 tài khoản định dạng MD5 hex** và **4 tài khoản định dạng Plaintext**, hoàn toàn **0 tài khoản định dạng PBKDF2**. Mọi tài khoản cần cơ chế rehash-on-login hoạt động chính xác để tự động chuyển đổi khi đăng nhập thành công.

---

## 4. Ma trận Phủ quyền Toàn bộ Endpoint của `CinemaBD.Api`

Toàn bộ **30 Controllers** với hơn 60 endpoints trong `CinemaBD.Api` được khảo sát trực tiếp từ mã nguồn:

### 4.1. Nhóm API Khách hàng và Công khai (`CinemaBD.Api/Controllers/`)

| Controller | Route | Method | Xác thực | Role / Policy | Kiểm tra IDOR (Chống BOLA) | Đánh giá an toàn |
|---|---|---|---|---|---|---|
| `AuthController` | `/api/auth/login` | POST | `[AllowAnonymous]` | Không | N/A | Rate limit `"auth"` (10/min). Thông báo lỗi gây account enumeration. |
| `AuthController` | `/api/auth/register` | POST | `[AllowAnonymous]` | Không | N/A | Rate limit `"auth"`. Thiếu policy độ dài/mật khẩu yếu. |
| `AuthController` | `/api/auth/google` | POST | `[AllowAnonymous]` | Không | N/A | Rate limit `"auth"`. Xác thực Google id_token hợp lệ. |
| `AuthController` | `/api/auth/logout` | POST | `[Authorize]` | Bất kỳ user có JWT | N/A | Thu hồi `jti` vào bảng `RevokedTokens`. Tốt. |
| `AccountController` | `/api/account/profile` | GET, PUT | `[Authorize]` (class) | User authenticated | **Có**: Lấy `userId` từ claim JWT (`sub`/`NameIdentifier`), không nhận từ client. | An toàn trước IDOR. |
| `AccountController` | `/api/account/loyalty` | GET | `[Authorize]` (class) | User authenticated | **Có**: Lấy `userId` từ claim JWT. | An toàn trước IDOR. |
| `AccountController` | `/api/account/loyalty/preview-redeem` | POST | `[Authorize]` (class) | User authenticated | **Có**: Lấy `userId` từ claim JWT. | An toàn trước IDOR. |
| `AccountController` | `/api/account/history` | GET | `[Authorize]` (class) | User authenticated | **Có**: Lấy `userId` từ claim JWT. | An toàn trước IDOR. |
| `AccountTicketsController` | `/api/account/tickets` | GET | `[Authorize]` (class) | User authenticated | **Có**: Lấy `userId` từ claim JWT; có ký số HMAC cho mã QR vé trả về. | An toàn trước IDOR và giả mạo QR. |
| `CustomerVoucherController` | `/api/account/vouchers` | - | `[Authorize]` (class) | User authenticated | N/A | Controller rỗng (chưa có action nào). |
| `BookingsController` | `/api/bookings/showtimes/{id}/seats` | GET | **Không có attribute** | **Public by omission** | Không nhạy cảm (sơ đồ ghế công khai). | **Cần gán `[AllowAnonymous]` tường minh** khi bật FallbackPolicy. |
| `BookingsController` | `/api/bookings/hold-seats` | POST | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim JWT, khóa giao dịch bằng transaction Serializable. | Rate limit `"transactions"`. Tốt. |
| `BookingsController` | `/api/bookings/release-seats` | POST | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim JWT, chỉ nhả ghế do chính user giữ. | Rate limit `"transactions"`. Tốt. |
| `BookingsController` | `/api/bookings/voucher-preview` | POST | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim JWT để kiểm tra điều kiện voucher cá nhân. | Rate limit `"transactions"`. Tốt. |
| `BookingsController` | `/api/bookings/checkout` | POST | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim JWT. ReturnUrl được kiểm tra whitelist cấu hình server. | Rate limit `"transactions"`. Tốt. |
| `BookingsController` | `/api/bookings/refund-request` | POST | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim JWT; service kiểm tra vé thuộc về user. | Rate limit `"transactions"`. Tốt. |
| `BookingsController` | `/api/bookings/invoice/{txnRef}` | GET | `[Authorize]` | User authenticated | **Có**: Service `GetInvoiceAsync` ràng buộc `userId` trừ khi là admin. | Chống IDOR xem hóa đơn chéo. |
| `PaymentsController` | `/api/payments/vnpay/create` | POST | `[Authorize]` | User authenticated | **Có**: Kiểm tra hóa đơn thuộc về `userId` đang đăng nhập (hoặc Admin). | Rate limit `"transactions"`. Chống IDOR tốt. |
| `PaymentsController` | `/api/payments/vnpay-return` | GET | **Không có attribute** | Public callback | N/A | Xác minh chữ ký số VNPay trước khi xử lý DB. Deep link an toàn cho Android. |
| `PaymentsController` | `/api/payments/vnpay-ipn` | GET | **Không có attribute** | Public server-to-server | N/A | Xác minh chữ ký số VNPay, kiểm tra số tiền, chống lặp trạng thái (idempotent). |
| `PaymentsController` | `/api/payments/demo-confirm` | GET | **Không có attribute** | Public (Dev only) | N/A | **Kiểm tra guard**: `if (!_environment.IsDevelopment()) return NotFound()`. An toàn khi production. |
| `MoviesController` | `/api/movies` (all actions) | GET | **Không có attribute** | **Public by omission** | Dữ liệu phim công khai. | **Cần gán `[AllowAnonymous]` tường minh**. |
| `CombosController` | `/api/combos` (all actions) | GET | **Không có attribute** | **Public by omission** | Dữ liệu combo công khai. | **Cần gán `[AllowAnonymous]` tường minh**. |
| `ArticlesController` | `/api/articles` (all actions) | GET | **Không có attribute** | **Public by omission** | Tin tức công khai. | **Cần gán `[AllowAnonymous]` tường minh**. |
| `EventsController` | `/api/events` (all actions) | GET | **Không có attribute** | **Public by omission** | Sự kiện công khai. | **Cần gán `[AllowAnonymous]` tường minh**. |
| `ReviewsController` | `/api/reviews/movie/{movieId}` | GET | **Không có attribute** | **Public by omission** | Đánh giá công khai. | **Cần gán `[AllowAnonymous]` tường minh**. |
| `ReviewsController` | `/api/reviews/movie/{id}/eligibility` | GET | `[Authorize]` | User authenticated | **Có**: Lấy `userId` từ claim để kiểm tra đã xem phim chưa. | Tốt. |
| `ReviewsController` | `/api/reviews` | POST | `[Authorize]` | User authenticated | **Có**: Gán đánh giá theo `userId` từ token claim. | Tốt. |
| `ReviewsController` | `/api/reviews/admin*` (3 actions) | GET, POST, DELETE | `[Authorize(Policy = "AdminOnly")]` | Admin | **Thiếu phân quyền chi tiết**: Bất kỳ ai có role "Admin" đều xóa/ẩn được review. | **Nằm sai vị trí cấu trúc**: Đặt action Admin trong Client Controller thay vì `Admin*Controller`. |

#### 4.1.1. Chi tiết Bằng chứng Mã nguồn Kiểm tra Quyền sở hữu (Chống IDOR / BOLA)

Các luồng giao dịch tài chính và đặt vé nhạy cảm được bảo vệ chặt chẽ từ Controller xuống Service tầng Domain/Infrastructure:

1. **`hold-seats` (Giữ ghế)**:
   - **Controller**: `CinemaBD.Api/Controllers/BookingsController.cs:47-49`: Trích xuất `userId` trực tiếp từ JWT claim `sub` / `NameIdentifier`, không tin cậy dữ liệu truyền lên từ request body:
     ```csharp
     var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
     if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(...);
     ```
   - **Service**: `CinemaBD.Infrastructure/Services/BookingService.cs:53-58`: Ngăn chặn xung đột và chiếm ghế của người khác:
     ```csharp
     var unavailableSeats = await _db.Tickets
         .Where(v => v.MaSuatChieu == showtimeId
             && selectedSeats.Contains(v.MaGhe)
             && (v.TrangThai == "Paid" || (v.TrangThai == "Pending" && v.MaKH != userId && v.NgayDat > holdExpiredBefore)))
         .Select(v => v.MaGhe).ToListAsync(cancellationToken);
     ```
   - Dòng `66-72`: Chỉ gia hạn thời gian giữ ghế cho chính user đang giữ (`v.MaKH == userId`).
   - Dòng `80`: Gán vé giữ mới bắt buộc với `MaKH = userId`.

2. **`release-seats` (Nhả ghế)**:
   - **Controller**: `CinemaBD.Api/Controllers/BookingsController.cs:60-62`: Đọc `userId` từ token claim.
   - **Service**: `CinemaBD.Infrastructure/Services/BookingService.cs:102-106`: Ràng buộc điều kiện xóa vé chỉ đối với vé do chính `userId` đang giữ:
     ```csharp
     var ownPendingTickets = await _db.Tickets
         .Where(v => v.MaSuatChieu == showtimeId && selectedSeats.Contains(v.MaGhe) && v.MaKH == userId && v.TrangThai == "Pending" && v.GatewayTxnRef == null)
         .ToListAsync(cancellationToken);
     _db.Tickets.RemoveRange(ownPendingTickets);
     ```
     Nếu kẻ tấn công gửi mã ghế của người khác, `ownPendingTickets` trả về rỗng, ngăn chặn hành vi nhả ghế phá hoại (Denial of Inventory).

3. **`checkout` (Tạo phiên thanh toán)**:
   - **Controller**: `CinemaBD.Api/Controllers/BookingsController.cs:93-95`: Đọc `userId` từ token claim.
   - **Service**: `CinemaBD.Infrastructure/Services/BookingService.cs:140-144`: Kiểm tra lại danh sách ghế không bị user khác giữ (`v.MaKH != userId`).
   - Dòng `159-178`: Cập nhật vé `v.MaKH == userId` sang mã `GatewayTxnRef = txnRef`.
   - Dòng `215-216 & 223-226`: Kiểm tra quyền sở hữu voucher cá nhân (`v.MaKH == userId || v.MaKH == "ALL" || v.MaKH == "*"`), ngăn chặn việc dùng trộm voucher của khách hàng khác.
   - Dòng `272-283`: Giảm trừ điểm thưởng loyalty chính chủ của `userId`.

4. **`refund-request` (Yêu cầu hoàn vé)**:
   - **Controller**: `CinemaBD.Api/Controllers/BookingsController.cs:108-110`: Đọc `userId` từ token claim.
   - **Service**: `CinemaBD.Infrastructure/Services/BookingService.cs:381-384`: Kiểm tra chặt chẽ hóa đơn và vé thuộc sở hữu của người yêu cầu:
     ```csharp
     var invoice = await _db.Invoices.FirstOrDefaultAsync(x => (x.GatewayTxnRef == txnRef || x.MaHD == txnRef) && x.MaKH == userId, cancellationToken);
     var effectiveTxnRef = invoice?.GatewayTxnRef ?? txnRef;
     var paidStatuses = new[] { PaymentStatuses.Paid, "Success", "Thành công", "Đã thanh toán" };
     var ticketsQuery = _db.Tickets.Where(x => x.MaKH == userId && x.GatewayTxnRef == effectiveTxnRef && paidStatuses.Contains(x.TrangThai));
     ```
     Nếu User A yêu cầu hoàn vé thuộc hóa đơn của User B, câu truy vấn trả về rỗng và hệ thống báo lỗi từ chối tại dòng 390.

### 4.2. Nhóm API Quản trị (`CinemaBD.Api/Controllers/Admin/`)

Tất cả 18 controller quản trị dưới đây (trừ `AdminAuthController`) đều được gắn `[Authorize(Policy = "AdminOnly")]` ở cấp Controller:

| Controller | Tuyến đường chính | Các HTTP Methods | Phân quyền hiện tại | Rủi ro / Điểm yếu bảo mật nhận diện |
|---|---|---|---|---|
| `AdminAuthController` | `/api/admin/auth/login` | POST | `[AllowAnonymous]` | Enumeration lỗi đăng nhập; thiếu endpoint logout; rehash MD5 bị bỏ quên. |
| `AdminDashboardController` | `/api/admin/dashboard` | GET | `AdminOnly` | Mọi tài khoản admin đều xem được toàn bộ chỉ số tài chính/doanh thu. |
| `AdminMoviesController` | `/api/admin/movies` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Movies.Manage` / `Movies.View`. |
| `AdminShowtimesController` | `/api/admin/showtimes` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Showtimes.Manage`. Hành động hủy/tạo lịch ảnh hưởng lớn đến vận hành rạp. |
| `AdminCinemasController` | `/api/admin/cinemas` | GET, POST | `AdminOnly` | Thiếu quyền `Cinemas.Manage`. |
| `AdminRoomsController` | `/api/admin/rooms` | GET, POST | `AdminOnly` | Thiếu quyền `Rooms.Manage`. |
| `AdminSeatsController` | `/api/admin/seats` | GET, POST | `AdminOnly` | Thiếu quyền `Seats.Manage`. Bật/tắt ghế ảnh hưởng bán vé trực tiếp. |
| `AdminGenresController` | `/api/admin/genres` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Genres.Manage`. |
| `AdminCombosController` | `/api/admin/combos` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Combos.Manage`. |
| `AdminArticlesController` | `/api/admin/articles` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Articles.Manage`. |
| `AdminEventsController` | `/api/admin/events` | GET, POST, PUT, DELETE | `AdminOnly` | Thiếu quyền `Events.Manage`. |
| `AdminCustomersController` | `/api/admin/customers` | GET, POST, PUT, DELETE | `AdminOnly` | **Rủi ro PII (PR.DS-5)**: Mọi admin đều xem/sửa/xóa thông tin cá nhân khách hàng. Thiếu quyền `Customers.Manage`. |
| `AdminEmployeesController` | `/api/admin/employees` | GET, POST, PUT, DELETE | `AdminOnly` | **Rủi ro nhân sự**: Nhân viên có thể tự sửa thông tin của mình hoặc nhân viên khác. Thiếu quyền `Employees.Manage`. |
| `AdminRolesController` | `/api/admin/roles` | GET, POST, PUT, DELETE | `AdminOnly` | **Rủi ro nâng quyền (Elevation of Privilege)**: Bất kỳ admin nào cũng có thể tự tạo role, tự gán quyền cho role của mình. Thiếu kiểm soát `Roles.Manage` và phân tách quyền SuperAdmin. |
| `AdminInvoicesController` | `/api/admin/invoices` | GET, POST (`check-in`, `sync`) | `AdminOnly` | Nhân viên soát vé cần quyền `Invoices.CheckIn`, trong khi kế toán cần `Invoices.View`/`Invoices.Sync`. Hiện tại gộp chung `AdminOnly`. |
| `AdminLoyaltyPointsController` | `/api/admin/loyalty-points` | GET, POST (`adjust`) | `AdminOnly` | **Rủi ro gian lận tài chính**: Mọi admin đều có thể cộng/trừ điểm thưởng mà không cần lý do bắt buộc, không lưu vết ai đã điều chỉnh. Cần quyền `Loyalty.Adjust`. |
| `AdminRefundsController` | `/api/admin/refunds` | GET, POST (`approve`, `reject`) | `AdminOnly` | **Rủi ro gian lận tiền vé (Separation of Duties)**: Không kiểm tra người duyệt hoàn tiền có phải là người tạo yêu cầu hay không; không lưu vết Admin ID đã duyệt. Cần quyền `Refunds.Approve`. |
| `AdminStatisticsController` | `/api/admin/statistics/revenue` | GET | `AdminOnly` | Số liệu doanh thu mật cần giới hạn cho Ban Giám đốc/Kế toán (`Statistics.View`). |
| `AdminVouchersController` | `/api/admin/vouchers` | GET, POST, PUT, DELETE, `validate` | `AdminOnly` | **Rủi ro mã giảm giá**: Tạo voucher giảm giá tùy ý không qua kiểm soát ngân sách. Cần quyền `Vouchers.Manage`. |

### 4.3. Xác nhận Tình trạng `[Authorize]` trên Toàn bộ Controller Admin và Rà soát Endpoint Công khai

Theo yêu cầu kiểm tra toàn diện:

1. **Xác nhận `[Authorize]` ở cấp class của các Controller Admin**:
   - **Đã kiểm tra 19/19 Controller trong `CinemaBD.Api/Controllers/Admin/`**:
     - **18 Controller nghiệp vụ quản trị** (`AdminArticles`, `AdminCinemas`, `AdminCombos`, `AdminCustomers`, `AdminDashboard`, `AdminEmployees`, `AdminEvents`, `AdminGenres`, `AdminInvoices`, `AdminLoyaltyPoints`, `AdminMovies`, `AdminRefunds`, `AdminRoles`, `AdminRooms`, `AdminSeats`, `AdminShowtimes`, `AdminStatistics`, `AdminVouchers`) **100% ĐỀU ĐÃ CÓ** attribute `[Authorize(Policy = "AdminOnly")]` ở cấp class.
     - **1 Controller ngoại lệ**: `AdminAuthController` **không có** `[Authorize]` ở cấp class vì đây là controller phục vụ đăng nhập admin. Action duy nhất của nó là `POST /api/admin/auth/login` và action này đã được gán tường minh `[AllowAnonymous]`.
   - **Controller ngoài thư mục Admin có action quản trị**: `ReviewsController` (tại `Controllers/ReviewsController.cs`) chứa 3 action admin (`GET /api/reviews/admin`, `POST /api/reviews/admin/{id}/toggle-hidden`, `DELETE /api/reviews/admin/{id}`) đều đã được bảo vệ bằng `[Authorize(Policy = "AdminOnly")]` ở cấp method.

2. **Rà soát các endpoint KHÔNG có `[Authorize]` mà không nên công khai**:
   - **KẾT QUẢ XÁC NHẬN: KHÔNG CÓ ENDPOINT NỘI BỘ / NHẠY CẢM NÀO THIẾU `[Authorize]`.**
   - Mọi endpoint không có `[Authorize]` trong hệ thống đều là:
     - Danh mục phim công khai: `GET /api/movies*` (`MoviesController`)
     - Danh mục combo công khai: `GET /api/combos*` (`CombosController`)
     - Tin tức bài viết công khai: `GET /api/articles*` (`ArticlesController`)
     - Sự kiện công khai: `GET /api/events*` (`EventsController`)
     - Đánh giá phim công khai: `GET /api/reviews/movie/{movieId}` (`ReviewsController`)
     - Sơ đồ ghế công khai: `GET /api/bookings/showtimes/{id}/seats` (`BookingsController`)
     - Callback cổng thanh toán: `GET /api/payments/vnpay-return`, `GET /api/payments/vnpay-ipn` (`PaymentsController`)
     - Endpoint thử nghiệm dev: `GET /api/payments/demo-confirm` (có guard `!_environment.IsDevelopment() -> NotFound()`)
     - Endpoint đăng nhập/đăng ký: `POST /api/auth/login`, `register`, `google`, `admin/auth/login` (đều có `[AllowAnonymous]`).
   - **Khoảng cách cần xử lý trong Phase 1**: Các endpoint danh mục công khai nói trên đang ở trạng thái **Public by omission** (công khai vì quên khai báo attribute). Khi Phase 1 kích hoạt `FallbackPolicy = RequireAuthenticatedUser`, toàn bộ các endpoint này phải được khai báo `[AllowAnonymous]` một cách tường minh để không bị từ chối truy cập ngoài ý muốn.

---

## 5. Cơ chế Permission Hiện có của `AdminRolesController`

Căn cứ: `AdminRolesController.cs`, `AdminRoleService.cs`, `AppDbContext.cs`.

### 5.1. Mô hình CSDL hiện có
Hệ thống đã có cấu trúc dữ liệu phân quyền khá hoàn chỉnh trong CSDL SQL Server:
- Bảng `Roles` (`LegacyRole` / `CHUCVU`): `MaCV`, `TenChucVu`, `IsMaster`, `IsActive`, `CreatedAt`, `UpdatedAt`.
- Bảng `Permissions` (`LegacyPermission` / `CHUCNANG`): `MaCN`, `TenChucNang`.
- Bảng `RolePermissions` (`LegacyRolePermission` / `PHANQUYEN`): Khóa chính kép `(MaCV, MaCN)`.
- Bảng `Admins` (`LegacyAdmin` / `ADMIN`): `AdminID`, `Username`, `Password`, `MaCV`, `Role`, `IsActive`...
- Bảng `Employees` (`LegacyEmployee` / `NHANVIEN`): `MaNV`, `HoTen`, `MaCV`, `IsActive`...

### 5.2. Sự thật về việc thực thi quyền (Runtime Enforcement)
- **KẾT LUẬN QUAN TRỌNG NHẤT**: **CÁC PERMISSION HOÀN TOÀN CHƯA ĐƯỢC KIỂM TRA Ở RUNTIME.**
  - `AdminRolesController` chỉ đóng vai trò lưu trữ và cập nhật bảng `RolePermissions` trong cơ sở dữ liệu.
  - Trong toàn bộ solution, **KHÔNG CÓ** bất kỳ `IAuthorizationPolicyProvider`, `AuthorizationHandler`, Action Filter hay middleware nào kiểm tra quyền chi tiết (permission claim hay permission database check).
  - Khởi động ứng dụng (`CinemaBD.Api/Program.cs:145-148`) chỉ đăng ký duy nhất một policy:
    ```csharp
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });
    ```
  - Khi quản trị viên đăng nhập (`AdminAuthApiService.cs:39` và `JwtTokenService.cs:26-34`), do ID có tiền tố `admin:`, JWT luôn được gán claim `Role = "Admin"`.
  - **Hệ quả thực tế**: Mọi tài khoản nhân viên hay quản trị viên đăng nhập thành công vào hệ thống đều sở hữu quyền lực ngang nhau (toàn quyền trên tất cả 19 controller admin), biến cơ chế phân quyền trong CSDL thành "quyền trên giấy", không có giá trị bảo vệ thực tế.

---

## 6. Quản lý Phiên (Session Management)

| Hạng mục | Hiện trạng | Khoảng cách so với yêu cầu chuẩn IAM |
|---|---|---|
| **Thu hồi Token khi Logout** | `AuthController.Logout` đã đọc `jti` và `exp` từ JWT, lưu vào bảng `RevokedTokens`. Middleware `OnTokenValidated` kiểm tra CSDL và từ chối token hợp lệ. | Đã giải quyết được nhược điểm của stateless JWT thông thường. Tuy nhiên: **`AdminAuthController` không có chức năng logout**, admin không thể thu hồi token khi đăng xuất. |
| **Refresh Token** | **Hoàn toàn chưa có**. Client chỉ nhận 1 Access Token có hạn 12 giờ. | Khi token hết hạn, client buộc phải hỏi lại mật khẩu người dùng; không thể xoay vòng token ngầm; không thể phát hiện token reuse. |
| **Quản lý phiên theo thiết bị** | Không có bảng lưu thông tin thiết bị, IP đăng nhập, hay refresh token family. | Người dùng không thể xem danh sách thiết bị đang đăng nhập hoặc chọn "Đăng xuất khỏi tất cả các thiết bị". |
| **Hiệu lực tức thì khi đổi quyền/khóa tài khoản** | Token có hạn 12 giờ. Nếu admin bị khóa tài khoản (`IsActive = false`) hoặc bị đổi vai trò, token cũ vẫn dùng được bình thường trong suốt 12 giờ vì `OnTokenValidated` chỉ kiểm tra bảng `RevokedTokens` chứ không kiểm tra trạng thái user trong DB. | Cần cơ chế `SecurityStamp` hoặc `TokenVersion` để vô hiệu hóa phiên ngay khi tài khoản bị thay đổi quyền/khóa. |

---

## 7. Khảo sát CORS, Rate Limiting, Security Headers, Web & Android Security

### 7.1. Cấu hình trên `CinemaBD.Api`
- **CORS**: Được đăng ký với policy `"WebClients"` (`Program.cs:17-35`). Kiểm tra nghiêm ngặt: origin phải là HTTPS tuyệt đối (ngoại trừ môi trường Development), chỉ cho phép các method `GET, POST, PUT, DELETE, OPTIONS` và các header `Authorization, Content-Type, X-Return-Url`. Đạt chuẩn an toàn cao.
- **Rate Limiting**: Đã tích hợp `Microsoft.AspNetCore.RateLimiting` (`Program.cs:36-59`):
  - Policy `"auth"`: Fixed window 10 requests / 1 phút theo IP. Gắn trên login, register, google.
  - Policy `"transactions"`: Fixed window 20 requests / 1 phút theo IP. Gắn trên hold-seats, release-seats, checkout, vnpay.
  - Trả về mã HTTP `429 Too Many Requests` kèm `Retry-After: 60` và ProblemDetails tiếng Việt.
- **Security Headers & HTTPS**:
  - Middleware tùy biến (`Program.cs:176-191`) tự động gán: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy` phân tách cho Swagger và API data.
  - Khi chạy ngoài Development: Tự động thêm `Strict-Transport-Security: max-age=31536000; includeSubDomains`. Đạt chuẩn NIST PR.IP-1.

### 7.2. Cấu hình trên `CinemaBD.Web` — Chi tiết các Điểm Còn Thiếu

Rà soát toàn diện ứng dụng `CinemaBD.Web` (MVC Frontend) cho thấy 6 nhóm thiếu sót kỹ thuật cụ thể:

1. **Bảo mật Cookie (PR.DS, PR.AC)**:
   - **Session Cookie** (`Program.cs:41-45`): Mới chỉ cấu hình `options.Cookie.HttpOnly = true`, `IsEssential = true`. **Thiếu** `SecurePolicy = CookieSecurePolicy.Always` (hoặc `SameAsRequest`) và **thiếu** `SameSite = SameSiteMode.Lax` (hoặc `Strict`).
   - **Authentication Cookie** (`Program.cs:88` `.AddCookie("Cookies")`): Khai báo rỗng không có options, sử dụng toàn bộ giá trị mặc định của ASP.NET Core: **thiếu** `HttpOnly = true`, **thiếu** `SecurePolicy = CookieSecurePolicy.Always`, **thiếu** `SameSite = SameSiteMode.Lax`, **thiếu** `ExpireTimeSpan` và `SlidingExpiration`.
2. **Security Headers (PR.IP-1, PR.PT-3)**:
   - Trong khi `CinemaBD.Api` đã có middleware tự động thêm `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, và `CSP`, thì `CinemaBD.Web` **hoàn toàn chưa có middleware Security Headers**. Ứng dụng Web MVC render giao diện HTML trực tiếp cho người dùng, do đó việc thiếu `X-Frame-Options` tạo ra nguy cơ tấn công Clickjacking, và thiếu `nosniff` tạo rủi ro MIME-type sniffing.
3. **Rate Limiting (PR.AC-7, DE.AE-2)**:
   - `CinemaBD.Web` **hoàn toàn chưa đăng ký middleware Rate Limiting**. Các form đăng nhập Web (`/account/login`, `/Admin/AdminAccount/Login`) và đăng ký (`/account/register`) không bị giới hạn tần suất ở tầng Web, kẻ tấn công có thể brute-force trực tiếp vào Web app.
4. **Bảo vệ CSRF / Antiforgery (PR.AC-3, PR.IP-1)**:
   - Chưa đăng ký global filter `AutoValidateAntiforgeryTokenAttribute` trong `AddControllersWithViews()`.
   - Endpoint đăng xuất của khách hàng (`AccountController.Logout`: dòng 263) là `[HttpGet]` chứ không phải `[HttpPost] [ValidateAntiForgeryToken]`, dễ bị tấn công Logout CSRF khiến người dùng bị đăng xuất ngoài ý muốn qua thẻ `<img>` hoặc link độc hại.
5. **Sai lệch Kiến trúc Luồng Xác thực Google (Inconsistency / Bypassing API Validation)**:
   - `CinemaBD.Web/Controllers/AccountController.cs:53-56`: Sau khi nhận callback Google từ cookie, controller tự động sinh một mật khẩu giả lập theo định dạng `"GoogleAuth_" + googleId`, sau đó gọi API đăng nhập thông thường (`_apiClient.LoginAsync(email, googlePassword)` hoặc `RegisterAsync`). Luồng này **bỏ qua hoàn toàn** endpoint chuẩn `POST /api/auth/google` của `CinemaBD.Api` (vốn có xác thực chữ ký Google ID token qua thư viện `GoogleJsonWebSignature`).
6. **Thu hồi Phiên Quản trị và Phân quyền Giao diện**:
   - `AdminAccountController.Logout` (`CinemaBD.Web/Areas/Admin/Controllers/AdminAccountController.cs:47-51`) chỉ thực hiện `HttpContext.Session.Clear()`, không gọi bất kỳ API thu hồi token nào trên `CinemaBD.Api`. Access Token 12 giờ của Admin vẫn hoàn toàn hợp lệ trên API nếu bị rò rỉ trước đó.
   - `AdminPermissionAttribute` (`CinemaBD.Web/Core/AdminPermissionAttribute.cs` và `AdminNavigationService.cs:63-73`): Nếu API trả về danh sách permission rỗng hoặc gặp ngoại lệ mạng (catch), hệ thống tự động gán `AllowAll = true`, tiềm ẩn rủi ro Fail-Open (mở toang quyền khi có lỗi).

### 7.3. Ứng dụng Di động Android (`android/`)
- **Lưu trữ Token**: `TokenDataStore.kt:27-42` lưu JWT trong `preferencesDataStore(name = "token_prefs")` dưới dạng chuỗi văn bản thuần (plain XML/protobuf), **chưa sử dụng** `EncryptedSharedPreferences` hay Android Keystore.
- **Xử lý 401 Unauthorized**: `AuthInterceptor.kt:38-40` bắt phản hồi `response.code == 401` và gọi `sessionManager.notifySessionExpiredAsync()` để xóa session và yêu cầu người dùng đăng nhập lại.
- **Hạ tầng Refresh Token**: File `TokenDataStore.kt` đã khai báo sẵn `REFRESH_TOKEN_KEY`, nhưng `AuthInterceptor` chưa có logic tự động gọi API refresh token để làm mới phiên làm việc.

---

## 8. Rà soát Nhật ký (Logging) và Dấu vết Kiểm toán (Audit Trail)

- **Xử lý Lỗi và Ghi Log**:
  - `CinemaBD.Api/Middleware/ExceptionHandlingMiddleware.cs:16-37` bắt các ngoại lệ `ArgumentException`, `InvalidOperationException`, `Exception`.
  - Log ghi nhận: Chỉ ghi `context.Request.Path` và `context.TraceIdentifier`.
  - Phản hồi người dùng: Đóng gói dưới dạng RFC 7807 `ProblemDetails` (`application/problem+json`) bằng tiếng Việt chung chung ("Yêu cầu không hợp lệ", "Không thể xử lý yêu cầu", "Đã xảy ra lỗi máy chủ"), che giấu hoàn toàn stack trace và thông tin hạ tầng nội bộ.
  - Không ghi log mật khẩu, JWT token hoặc thông tin PII nhạy cảm vào log file.
- **Tập tin `audit_actions.txt`**:
  - Đây là file văn bản liệt kê danh mục 100+ action MVC của các Controller quản trị trong `CinemaBD.Web/Areas/Admin` (dùng để rà soát kiểm kê mã nguồn), không phải là log hệ thống đang chạy.
- **Bảng `AuditLog` trong CSDL**:
  - **CHƯA CÓ**. CSDL hiện tại chỉ có bảng `HOADON_LICHSU` (`LegacyInvoiceHistory`) ghi nhận lịch sử trạng thái hóa đơn. Hoàn toàn chưa có bảng ghi nhận nhật ký kiểm toán độc lập (Audit Trail) cho các sự kiện IAM và quản trị quan trọng: Đăng nhập/đăng xuất, thay đổi role/permission, duyệt hoàn tiền, chỉnh sửa điểm thưởng, check-in vé.

---

## 9. Vệ sinh Repository (Repo Hygiene)

Kết quả kiểm tra qua `git ls-files` và cấu hình `.gitignore`:

| Tệp / Mẫu kiểm tra | Trạng thái theo dõi trong Git | Ghi chú an toàn |
|---|---|---|
| `.env` | **Không bị theo dõi** | Đã được chặn trong `.gitignore`. File `.env.example` chỉ chứa placeholder, không có bí mật thật. |
| `certs/*.pfx` | **Không bị theo dõi** | File `certs/cinemabd-dev.pfx` nằm trên ổ đĩa nhưng đã được `.gitignore` chặn (`certs/`, `*.pfx`). |
| `*.log`, `build_*.log` | **Không bị theo dõi** | Các file log build (`build_tmp.log`, `build_check.log`...) đều được `.gitignore` chặn. |
| `hs_err_pid*.log` | **Không bị theo dõi** | File crash log `android/hs_err_pid34236.log` đã được `.gitignore` chặn. |
| `local.properties` | **Không bị theo dõi** | File `android/local.properties` chứa đường dẫn Android SDK cục bộ, không bị git theo dõi. |
| `.vs/` | **Không bị theo dõi** | Thư mục cache Visual Studio đã được chặn. |
| **`.idea/`** | **ĐANG BỊ THEO DÕI (9 FILES)** | Các tệp cấu hình IDE Android Studio/IntelliJ đang nằm trong git index: `.idea/.gitignore`, `.idea/CinemaBD.iml`, `.idea/caches/deviceStreaming.xml`, `.idea/deviceManager.xml`, `.idea/markdown.xml`, `.idea/misc.xml`, `.idea/modules.xml`, `.idea/vcs.xml`. *(Chỉ báo cáo theo mục 9 của Phase 0, không tự ý xóa lịch sử)*. |

### 9.1. Báo cáo Kiểm tra Lịch sử Commit Toàn bộ Repository (Git Commit History)

Kiểm tra chuyên sâu bằng lệnh `git log --all --full-history` trên toàn bộ các nhánh:

1. **Tệp cấu hình môi trường `.env`**:
   - Lệnh: `git log --all --full-history -- ".env"`
   - Kết quả: **Hoàn toàn không có commit nào chứa tệp `.env` trong toàn bộ lịch sử git**. Trong repo chỉ có tệp `.env.example` chứa placeholder cấu hình.
2. **Chứng chỉ số / Khóa riêng `*.pfx`**:
   - Lệnh: `git log --all --full-history -- "*.pfx"`
   - Kết quả: **Hoàn toàn không có commit nào chứa tệp `.pfx` trong lịch sử git**. Tệp `certs/cinemabd-dev.pfx` nằm trên máy phát triển nhưng đã được `.gitignore` chặn ngay từ đầu.
3. **Thuộc tính cục bộ Android `local.properties`**:
   - Lệnh: `git log --all --full-history -- "*local.properties*"`
   - Kết quả: **Hoàn toàn không có commit nào chứa `local.properties` trong lịch sử git**. Đường dẫn Android SDK và cấu hình build máy trạm chưa từng bị rò rỉ vào repo.

> **Đánh giá**: Repository đảm bảo an toàn tuyệt đối về việc không để lộ file chứa secret, chứng chỉ số và file cấu hình môi trường cục bộ trong lịch sử commit.

---

## 10. Xác thực Callback Thanh toán (VNPay / MoMo)

Căn cứ: `PaymentsController.cs:26-116`, `PaymentService.cs:175-365`, `VnPaySignatureValidator.cs`.

### 10.1. Cổng thanh toán VNPay
1. **Xác minh chữ ký số (Integrity / PR.DS-2)**:
   - Phương thức `ProcessVnPayCallbackAsync` gọi `_validator.Validate(query)` (sử dụng HMAC-SHA512 với `VnPay:HashSecret`) **TRƯỚC TIÊN**.
   - Nếu chữ ký không khớp: Ghi log cảnh báo `[SECURITY_AUDIT]` (không chứa secret) và lập tức từ chối, **không cập nhật CSDL**.
2. **Xác minh số tiền (Amount Validation)**:
   - Kiểm tra `vnp_Amount / 100` có khớp chính xác với `invoice.TongTien` trong CSDL hay không. Nếu sai lệch thì từ chối.
3. **Tính Idempotent (Chống xử lý lặp)**:
   - Kiểm tra nếu trạng thái hóa đơn/đơn hàng đã là `Paid` (`PaymentStatuses.IsPaid(currentStatus)`), hệ thống lập tức trả về kết quả thành công với mã IPN `00` ("Order already confirmed") mà **không thực hiện lại giao dịch DB hay ghi vé lặp**.
4. **Kiểm tra thời hạn giữ ghế**:
   - Nếu giao dịch thanh toán thành công nhưng quá hạn giữ ghế (5 phút), hệ thống tự động hủy trạng thái thanh toán và trả về mã hết hạn.

### 10.2. Cổng thanh toán MoMo
- Hiện tại chưa có endpoint IPN/Return chính thức nào của MoMo trong `PaymentsController` (chỉ có endpoint `demo-confirm` dùng kiểm thử nội bộ trong môi trường Development).

---

## 11. Tổng kết Khoảng cách Chính và Kiến nghị cho Phase 1

1. **Xác thực**:
   - Rút ngắn thời hạn Access Token (15 phút) và xây dựng hệ thống **Refresh Token xoay vòng** có phát hiện gian lận (Reuse Detection).
   - Bổ sung cơ chế **Khóa tài khoản tạm thời** (Account Lockout) khi đăng nhập sai nhiều lần liên tiếp.
   - Sửa lỗi rehash mật khẩu admin (đảm bảo nâng cấp từ MD5 sang PBKDF2 khi login).
   - Chuẩn hóa thông báo lỗi đăng nhập để chống lộ diện tài khoản (Account Enumeration).
2. **Phân quyền**:
   - Thiết lập **Deny-by-default** (`FallbackPolicy = RequireAuthenticatedUser`). Gắn `[AllowAnonymous]` rõ ràng cho các API công khai.
   - Hiện thực hóa cơ chế phân quyền thực tế: Xây dựng `[HasPermission("...")]` với `AuthorizationHandler` đọc quyền từ CSDL/MemoryCache để các permission gán trong `AdminRolesController` thực sự có hiệu lực thi hành.
   - Ngăn chặn leo thang đặc quyền trong quản lý role và tách biệt nhiệm vụ (duyệt hoàn tiền, chỉnh điểm loyalty).
3. **Quản lý Phiên & Dữ liệu**:
   - Bổ sung endpoint logout cho Admin; cơ chế vô hiệu hóa tức thì phiên làm việc khi tài khoản bị khóa/đổi vai trò.
   - Nâng cấp lưu trữ token trên Android sang `EncryptedSharedPreferences` / Android Keystore.
4. **Kiểm toán & Giám sát**:
   - Thiết kế và tạo bảng `AuditLog` ghi nhận đầy đủ các sự kiện an ninh quan trọng của toàn hệ thống theo chuẩn NIST DE.CM / PR.PT.
