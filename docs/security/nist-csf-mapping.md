# Bảng Ánh xạ Kiểm soát An ninh Thông tin theo Khung NIST CSF 1.1 — CinemaBD (IAM)

> Tài liệu này được xây dựng trong **Phase 1** theo đề cương `docs/nist-iam-prompt.md`.  
> Mục tiêu: Tổng hợp hiện trạng, xác định khoảng cách (Gap Analysis) và định hình các gói công việc (Work Packages - WP) cho hệ thống Quản lý Định danh & Kiểm soát Truy cập (IAM), kế thừa từ [`docs/NIST_1_IDENTIFY.md`](../NIST_1_IDENTIFY.md) và làm cơ sở đối chiếu cho các tài liệu chi tiết tiếp theo ([`docs/NIST_2_PROTECT.md`](../NIST_2_PROTECT.md), [`docs/NIST_3_DETECT.md`](../NIST_3_DETECT.md), [`docs/NIST_4_RESPOND.md`](../NIST_4_RESPOND.md), [`docs/NIST_5_RECOVER.md`](../NIST_5_RECOVER.md)).

---

## 1. Ma trận Ánh xạ Khung NIST CSF 1.1 với Kiến trúc IAM của CinemaBD

| Chức năng (Function) | Mã NIST (Subcategory) | Yêu cầu Kiểm soát An ninh | Hiện trạng tại CinemaBD (Phase 0) | Khoảng cách Nhận diện (Gap) | Gói Công việc | Bằng chứng Dự kiến (Code / Test / Config) |
|---|---|---|---|---|---|---|
| **IDENTIFY (ID)** | **ID.AM-2** | Kiểm kê phần mềm, nền tảng, dịch vụ ngoài và toàn bộ API endpoint. | Đã khảo sát 30 controller (19 Admin, 11 Client) với hơn 60 endpoints; tích hợp VNPay, MoMo, Google OAuth. | Nhiều endpoint công khai "by omission" (thiếu `[AllowAnonymous]` tường minh). | **WP2** | `docs/security/iam-baseline.md#4`, `PermissionsMatrixTests.cs` |
| **IDENTIFY (ID)** | **ID.RA-1** | Nhận diện lỗ hổng bảo mật của tài sản và dữ liệu hệ thống. | Kế thừa 16 lỗ hổng từ `NIST_1_IDENTIFY.md` (F-01 đến F-16). Bổ sung lỗ hổng logic rehash admin và hở quyền runtime. | Chưa có kiểm soát permission ở runtime; token 12h không có refresh; thiếu lockout. | **Phase 1** | `docs/security/iam-baseline.md#11`, `docs/NIST_1_IDENTIFY.md` |
| **IDENTIFY (ID)** | **ID.GV-1** | Ban hành và duy trì chính sách an toàn thông tin & phân quyền. | Đã có tài liệu kiến trúc kỹ thuật (`LAYERED_ARCHITECTURE.md`, `CLAUDE.md`). | Chưa có tài liệu chính sách IAM chính thức (vòng đời tài khoản, mật khẩu, MFA, rà soát quyền định kỳ). | **WP5** | `docs/security/iam-policy.md` |
| **PROTECT (PR)** | **PR.AC-1** | Quản lý định danh, cấp phát, băm mật khẩu hiện đại và thu hồi chứng thực. | Dùng PBKDF2 (`PasswordHasher<UserAccount>`) kèm prefix `IDENTITY:`. Tự động rehash MD5 khi login. Thu hồi token qua JTI `RevokedTokens` và phiên qua `IdentitySessions`. Đã có MFA TOTP (RFC 6238) cho Admin. | **Trạng thái: "Một phần" (Partial)**: NhanVien (chưa có tài khoản đăng nhập) và KHACHHANG chưa được triển khai MFA (ghi nhận vào Backlog). | **WP1 (QW-1), B5** | `AdminAuthApiService.cs`, `TotpMfaService.cs`, `IdentitySessionService.cs` |
| **PROTECT (PR)** | **PR.AC-3** | Kiểm soát truy cập từ xa và kênh truyền thông tin. | HTTPS/HSTS cho API ngoài dev; CORS `WebClients` kiểm tra origin nghiêm ngặt. | Web MVC thiếu security headers; cookie session/auth thiếu `SecurePolicy` và `SameSite`. | **WP3 (QW-4, QW-5)** | `CinemaBD.Web/Program.cs`, `SecurityHeadersMiddleware.cs` |
| **PROTECT (PR)** | **PR.AC-4** | Quản lý quyền tối thiểu (Least Privilege) và tách biệt nhiệm vụ (SoD). | CSDL có bảng `Roles`, `Permissions`, `RolePermissions`. Giao diện Web có `AdminPermissionAttribute`. | **CÁC PERMISSION HOÀN TOÀN CHƯA THỰC THI Ở RUNTIME API**. Mọi admin đều có toàn quyền. Web admin có nguy cơ Fail-Open. | **WP2 (Gói 1)** | `HasPermissionAttribute.cs`, `PermissionAuthorizationHandler.cs`, `Permissions.cs` |
| **PROTECT (PR)** | **PR.AC-6** | Định danh được gắn chặt chẽ với thông tin xác thực (Google OAuth). | API có `POST /api/auth/google` xác thực Google ID token qua thư viện chính thức. | Web MVC tự sinh password giả `"GoogleAuth_" + googleId`, gọi API password thông thường, phá vỡ luồng chuẩn. | **WP1, WP4** | `AccountController.cs:53`, `AuthController.cs:40` |
| **PROTECT (PR)** | **PR.AC-7** | Mức độ xác thực tương xứng với mức độ rủi ro (MFA, Rate Limiting, Lockout). | Rate limiter cho auth (IP & account) trên API. Chính sách khóa tài khoản tạm thời qua `AccountLockoutService` (5 lần sai, khóa 15 phút). MFA TOTP (RFC 6238) cho Admin: Secret mã hóa Data Protection, 10 recovery codes SHA-256, chống replay qua `LastUsedStep`, login 2 bước với `mfa_challenge` (5 phút), bắt buộc cho Master qua `Mfa:RequireForMaster`. | **Trạng thái: "Một phần" (Partial)**. Các khoảng cách còn lại: (1) Lockout còn in-memory cục bộ tiến trình; (2) NhanVien và KHACHHANG chưa có MFA (Backlog); (3) TOTP về bản chất không chống được tấn công Phishing thời gian thực (đề xuất WebAuthn/FIDO2). | **WP1, WP3, B5** | `TotpMfaService.cs`, `AdminAuthApiService.cs`, `AccountLockoutService.cs`, `MfaEnrollmentRestrictionFilter.cs` |
| **PROTECT (PR)** | **PR.DS-1** | Bảo vệ dữ liệu nhạy cảm lưu trữ (Data-at-Rest). | Khóa JWT/QR đọc từ biến môi trường/User Secrets; không lưu secret trong git. | Mật khẩu cũ trong CSDL dạng MD5/plaintext chưa được mã hóa lại; token trên Android lưu dạng plain XML. | **WP1, WP3** | `TokenDataStore.kt` (EncryptedSharedPreferences), `DatabaseInitializer.cs` |
| **PROTECT (PR)** | **PR.DS-2** | Bảo vệ tính toàn vẹn dữ liệu truyền tải (Data-in-Transit & Digital Signatures). | Chữ ký số HMAC-SHA256 cho mã QR vé (`IQrCodeSecurityService`); xác thực chữ ký HMAC-SHA512 VNPay callback. | Chưa hỗ trợ xoay khóa JWT (`kid`); chưa hỗ trợ fallback key cũ trong thời gian chuyển tiếp. | **WP3** | `HmacQrCodeSecurityService.cs`, `VnPaySignatureValidator.cs`, `JwtTokenService.cs` |
| **PROTECT (PR)** | **PR.DS-5** | Bảo vệ chống rò rỉ dữ liệu nhạy cảm và thông tin định danh (PII). | `ExceptionHandlingMiddleware` đóng gói ProblemDetails tiếng Việt, che giấu stack trace; log không in password. | Thông báo lỗi đăng nhập phân biệt "tài khoản không tồn tại" vs "sai mật khẩu" (Account Enumeration). | **WP1 (QW-2)** | `AuthService.cs`, `AdminAuthApiService.cs` |
| **PROTECT (PR)** | **PR.IP-1** | Cấu hình nền tảng an toàn (Security Baseline Configuration). | Deny-by-default chưa bật trên API (Public by omission); Web MVC thiếu toàn bộ Security Headers. | Cần thiết lập `FallbackPolicy = RequireAuthenticatedUser` và cấu hình đồng bộ bảo mật cho Web. | **WP2, WP3** | `CinemaBD.Api/Program.cs`, `CinemaBD.Web/Program.cs` |
| **PROTECT (PR)** | **PR.PT-1** | Cơ chế nhật ký kiểm toán (Audit Trail) được tích hợp trong hệ thống. | Chỉ có log lỗi trong middleware và bảng `HOADON_LICHSU` ghi nhận trạng thái hóa đơn. | **CHƯA CÓ bảng AuditLog** ghi nhận nhật ký kiểm toán an ninh cho đăng nhập, đổi quyền, duyệt hoàn tiền, chỉnh loyalty. | **WP4** | `AppDbContext.cs` (`AuditLog`), `AuditLogService.cs`, `AuditLogMiddleware.cs` |
| **PROTECT (PR)** | **PR.PT-3** | Giảm thiểu bề mặt tấn công qua nguyên tắc chức năng tối thiểu. | Endpoint `demo-confirm` đã có guard chỉ chạy trong `Environment.IsDevelopment()`. | Cần rà soát các endpoint dev và dọn dẹp các route/area thừa trước khi đóng gói production. | **WP3** | `PaymentsController.cs:110` |
| **DETECT (DE)** | **DE.AE-2** | Phân tích và phát hiện các sự kiện đăng nhập bất thường hoặc tấn công brute-force. | Chỉ có rate limiting 10 requests/phút theo địa chỉ IP. | Chưa phát hiện và cảnh báo nhiều lần đăng nhập thất bại trên cùng một tài khoản từ nhiều IP hoặc ngược lại. | **WP4** | `SecurityEventDetector.cs`, `AuditLogAnalyzerTests.cs` |
| **DETECT (DE)** | **DE.AE-3** | Thu thập và tương quan dữ liệu sự kiện an ninh từ nhiều nguồn. | Chưa có hệ thống tập trung gom log giữa API, Web và Client. | Cần cơ chế chuẩn hóa log sự kiện an ninh `[SECURITY_AUDIT]` và correlation ID xuyên suốt. | **WP4** | `ExceptionHandlingMiddleware.cs`, `CorrelationIdMiddleware.cs` |
| **DETECT (DE)** | **DE.CM-3** | Giám sát các quyền truy cập quản trị và thay đổi cấu hình phân quyền. | Thay đổi role/permission trong `AdminRolesController` chỉ cập nhật DB, không có log cảnh báo. | Mọi thao tác gán/gỡ quyền cần được ghi audit log và phát hiện leo thang đặc quyền. | **WP4** | `AdminRolesController.cs`, `AuditLog.cs` |
| **DETECT (DE)** | **DE.CM-7** | Giám sát mã nguồn và hoạt động trái phép (truy cập 403, vượt quyền). | Chưa có cơ chế đếm và cảnh báo khi có tần suất mã HTTP 403 Forbidden bất thường. | Cần ghi nhận mọi lần từ chối quyền (Forbidden) vào Audit Trail để phát hiện tấn công leo thang. | **WP2, WP4** | `PermissionAuthorizationHandler.cs` (Audit-Only & 403 Logging) |
| **RESPOND (RS)** | **RS.AN-1** | Phân tích và điều tra nguyên nhân sự cố an ninh định danh. | Chưa có quy trình điều tra khi phát hiện token hoặc tài khoản admin bị lộ. | Cần tài liệu Runbook hướng dẫn trích xuất audit log, xác định phạm vi tài khoản bị thỏa hiệp. | **WP5** | `docs/security/runbook-account-compromise.md` |
| **RESPOND (RS)** | **RS.MI-2** | Giảm thiểu và ngăn chặn sự cố lan rộng (Khóa tài khoản, thu hồi phiên tức thì). | `AuthController.Logout` đã thu hồi được JTI vào DB `RevokedTokens`. | Chưa có cơ chế khóa phiên Admin tức thì khi tài khoản bị vô hiệu hóa (`IsActive = false`); thiếu endpoint logout admin. | **WP1 (QW-3)** | `AdminAuthController.cs`, `RevokedTokenService.cs` |
| **RESPOND (RS)** | **RS.RP-1** | Kế hoạch phản ứng và quy trình xử lý sự cố an ninh thông tin. | Chưa có quy trình chuẩn khi phát hiện tấn công brute-force hoặc rò rỉ JWT key. | Cần xây dựng tài liệu kế hoạch ứng phó sự cố IAM cho đội ngũ vận hành. | **WP5** | `docs/NIST_4_RESPOND.md`, `docs/security/iam-policy.md` |
| **RECOVER (RC)** | **RC.RP-1** | Kế hoạch khôi phục và duy trì tính liên tục của hệ thống xác thực. | Đã có Dockerfile, script `CinemaBD.sql` và `DatabaseInitializer` khôi phục CSDL trống. | Cần kiểm tra tính toàn vẹn của dữ liệu phân quyền khi khôi phục từ backup; quy trình xoay key an toàn. | **WP5** | `docs/NIST_5_RECOVER.md` |

---

## 2. Kế hoạch Triển khai Chi tiết theo Gói Công việc (Work Packages)

### 2.1. Danh mục Quick-Wins (Triển khai Ngay — Rủi ro Thấp, Giá trị Bảo vệ Cao)

Những chỉnh sửa này có thể thực hiện độc lập, không làm thay đổi hợp đồng API hiện tại, giải quyết ngay các rủi ro đã xác định:

1. **QW-1: Sửa lỗi Rehash Mật khẩu Admin** (`CinemaBD.Infrastructure/Services/AdminAuthApiService.cs:30-35`):
   - Thay thế kiểm tra plaintext `string.Equals(admin.Password, password)` bằng `_passwordHasher.NeedsRehash(admin.Password)`.
   - Đảm bảo tài khoản admin đang mang hash MD5 sau khi login thành công sẽ tự động được nâng cấp lên PBKDF2 (`IDENTITY:`).
2. **QW-2: Chuẩn hóa Thông báo Lỗi Đăng nhập Chống Account Enumeration**:
   - Trong `AuthService.cs` và `AdminAuthApiService.cs`: Thay đổi các thông báo "Tài khoản không tồn tại" và "Sai mật khẩu" thành một thông báo chung: *"Tài khoản hoặc mật khẩu không chính xác."*
3. **QW-3: Bổ sung Endpoint Đăng xuất Quản trị** (`AdminAuthController.cs`):
   - Thêm `POST /api/admin/auth/logout` có `[Authorize]`, đọc `jti` và `exp` từ JWT của admin để lưu vào `RevokedTokens`.
   - Cập nhật Web MVC (`AdminAccountController.Logout`) gọi endpoint này trước khi xóa Session.
4. **QW-4: Bổ sung Thuộc tính An toàn cho Cookie của Web MVC** (`CinemaBD.Web/Program.cs`):
   - Cấu hình Session Cookie: `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`, `options.Cookie.SameSite = SameSiteMode.Lax`.
   - Cấu hình Cookie Authentication: `options.Cookie.HttpOnly = true`, `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`, `options.Cookie.SameSite = SameSiteMode.Lax`, `options.ExpireTimeSpan = TimeSpan.FromHours(8)`.
5. **QW-5: Bổ sung Middleware Security Headers cho Web MVC** (`CinemaBD.Web/Program.cs`):
   - Thêm header `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`.

---

### 2.2. Gói 1 (Ưu tiên Cao nhất): Thực thi Phân quyền Runtime + Seed SuperAdmin + FallbackPolicy (Chế độ Audit-Only)

> **Mục tiêu**: Xóa bỏ hoàn toàn tình trạng "quyền trên giấy", biến mọi permission trong CSDL thành kiểm soát thực tế tại runtime mà không làm gián đoạn hay khóa tài khoản admin đang hoạt động.

#### Bước 1: Danh mục Hằng số Permission Chuẩn hóa (`Permissions.cs`)
Tạo `CinemaBD.Application/Constants/Permissions.cs`:
```csharp
namespace CinemaBD.Application.Constants;

public static class Permissions
{
    // Phim & Suất chiếu
    public const string MoviesView = "Movies.View";
    public const string MoviesManage = "Movies.Manage";
    public const string ShowtimesManage = "Showtimes.Manage";
    public const string CinemasManage = "Cinemas.Manage";
    public const string RoomsManage = "Rooms.Manage";
    public const string SeatsManage = "Seats.Manage";
    public const string GenresManage = "Genres.Manage";

    // Bán hàng & Khuyến mãi
    public const string CombosManage = "Combos.Manage";
    public const string ArticlesManage = "Articles.Manage";
    public const string EventsManage = "Events.Manage";
    public const string VouchersManage = "Vouchers.Manage";

    // Vận hành & Tài chính
    public const string InvoicesView = "Invoices.View";
    public const string InvoicesCheckIn = "Invoices.CheckIn";
    public const string InvoicesSync = "Invoices.Sync";
    public const string RefundsApprove = "Refunds.Approve";
    public const string LoyaltyAdjust = "Loyalty.Adjust";
    public const string StatisticsView = "Statistics.View";

    // Quản trị hệ thống & Kiểm toán
    public const string RolesManage = "Roles.Manage";
    public const string EmployeesManage = "Employees.Manage";
    public const string CustomersManage = "Customers.Manage";
    public const string AuditView = "Audit.View";
}
```

#### Bước 2: Seed Idempotent Role SuperAdmin (Bảo toàn Quyền Admin Hiện tại)
- Trong `DatabaseInitializer.cs`:
  - Đồng bộ danh sách hằng số `Permissions` vào bảng `ChucNang` (`CHUCNANG`).
  - Đảm bảo Role `Quản trị hệ thống` (`MaCV = 5` hoặc role có `IsMaster = 1`) được gán toàn bộ 100% permissions trong bảng `PhanQuyen`.
  - Tài khoản `admin` hiện hữu được đảm bảo liên kết với Role này.
  - Khi handler kiểm tra quyền: Nếu user thuộc Role có cờ `IsMaster == true`, handler lập tức cấp quyền bypass (`context.Succeed()`). **Cơ chế này loại bỏ hoàn toàn rủi ro khóa tài khoản admin đang vận hành.**

#### Bước 3: Triển khai Attribute `[HasPermission]` & Caching Tối ưu
- Tạo `HasPermissionAttribute(string permission)` kế thừa `AuthorizeAttribute`.
- Triển khai `PermissionRequirement(string permission)` và `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>`.
- Handler đọc danh sách quyền của Admin từ `IMemoryCache` (TTL 5 phút, khóa cache `admin_permissions:{adminId}`). Khi có thay đổi role/permission qua `AdminRolesController`, hệ thống tự động xóa cache liên quan để quyền mới có hiệu lực tức thì.

#### Bước 4: Chế độ Kiểm toán Trước khi Chặn Thật (Audit-Only / Dry-run Mode)
- Cấu hình qua `appsettings.json`:
  ```json
  "Security": {
    "Authorization": {
      "AuditOnly": true
    }
  }
  ```
- Logic kiểm tra trong `PermissionAuthorizationHandler`:
  ```csharp
  if (!hasPermission)
  {
      _logger.LogWarning("[SECURITY_AUDIT_FAIL] Actor {UserId} lacks permission {Permission} on path {Path}. Mode: AuditOnly={AuditOnly}",
          userId, requirement.Permission, httpContext.Request.Path, _options.AuditOnly);

      if (_options.AuditOnly)
      {
          context.Succeed(requirement); // Cho phép đi tiếp trong giai đoạn kiểm thử
          return Task.CompletedTask;
      }

      context.Fail(); // Chặn thật trả về 403 Forbidden khi AuditOnly = false
      return Task.CompletedTask;
  }
  ```
- **Lợi ích vận hành**: Cho phép triển khai và chạy thử trên môi trường staging/dev, quan sát log cảnh báo để kịp thời phát hiện endpoint nào bị gán nhầm quyền hoặc client nào bị ảnh hưởng trước khi chuyển sang chế độ chặn cứng.

#### Bước 5: Thiết lập Deny-by-default (`FallbackPolicy`)
- Gán tường minh `[AllowAnonymous]` cho toàn bộ endpoint công khai:
  - `MoviesController`, `CombosController`, `ArticlesController`, `EventsController`.
  - `ReviewsController:GetByMovie`.
  - `BookingsController:GetSeats`.
  - `PaymentsController:VnPayReturn`, `VnPayIpn`, `DemoConfirm`.
  - `AuthController:Login`, `Register`, `Google`.
  - `AdminAuthController:Login`.
- Cấu hình `FallbackPolicy` trong `CinemaBD.Api/Program.cs`:
  ```csharp
  builder.Services.AddAuthorization(options =>
  {
      options.FallbackPolicy = new AuthorizationPolicyBuilder()
          .RequireAuthenticatedUser()
          .Build();
  });
  ```
- Gắn `[HasPermission("...")]` cho toàn bộ các action trong 18 Controller quản trị và 3 action admin trong `ReviewsController`.

---

### 2.3. Gói 2: Quản lý Phiên, Refresh Token Xoay vòng & Khóa Tài khoản (WP1)

1. **Rút ngắn Thời hạn Access Token**:
   - Điều chỉnh Access Token từ 12 giờ xuống còn **15 phút**.
2. **Refresh Token Xoay vòng (Rotation) & Phát hiện Tái sử dụng (Reuse Detection)**:
   - Tạo bảng `RefreshTokens`: `Id`, `UserId`, `TokenHash` (SHA-256), `FamilyId` (GUID), `ExpiresAtUtc` (7 ngày), `CreatedAtUtc`, `RevokedAtUtc`, `ReplacedByTokenHash`, `DeviceInfo`, `IpAddress`.
   - Khi client gọi `/api/auth/refresh-token`:
     - Nếu token hợp lệ: Cấp cặp Access Token mới + Refresh Token mới, đánh dấu token cũ `ReplacedByTokenHash = newTokenHash`.
     - Nếu token đã bị thu hồi hoặc đã từng bị thay thế được gửi lại (dấu hiệu rò rỉ token / Replay Attack): Thu hồi lập tức toàn bộ FamilyId của chuỗi token này, bắt buộc đăng nhập lại từ đầu.
3. **Khóa Tài khoản Tạm thời (Account Lockout)**:
   - Thêm cột `AccessFailedCount` (int) và `LockoutEndUtc` (datetime2) vào bảng `Customer` và `Admin`.
   - Nếu nhập sai mật khẩu 5 lần liên tiếp: Khóa tài khoản trong 15 phút. Mỗi lần đăng nhập đúng sẽ đặt lại `AccessFailedCount = 0`.
4. **Xác thực Đa yếu tố MFA (TOTP)**:
   - Sử dụng thư viện `Otp.NET` (chuẩn RFC 6238, HMAC-SHA1 TOTP).
   - Bắt buộc kích hoạt cho toàn bộ tài khoản Admin và Nhân viên. Tùy chọn bật/tắt cho Khách hàng.
   - Hỗ trợ 5 mã khôi phục sử dụng một lần (Backup Codes) lưu dạng băm trong DB.

---

### 2.4. Gói 3: Nhật ký Kiểm toán Độc lập (Audit Trail) & Phát hiện Sự cố (WP4)

1. **Tạo Bảng `AuditLogs` trong CSDL (Append-Only)**:
   - Các trường: `Id` (long), `TimestampUtc`, `ActorId`, `ActorType` (Admin/Customer/Anonymous), `Action`, `Resource`, `StatusCode`, `IpAddress`, `UserAgent`, `TraceIdentifier`, `DetailsJson`.
2. **Ghi nhận các Sự kiện An ninh Trọng yếu**:
   - Xác thực: Đăng nhập thành công, đăng nhập thất bại, khóa tài khoản, đăng xuất, đổi mật khẩu, kích hoạt MFA.
   - Phân quyền: Mọi thao tác gán/gỡ role, chỉnh sửa permission, tạo mới/vô hiệu hóa admin và nhân viên.
   - Giao dịch nhạy cảm: Duyệt/từ chối hoàn tiền, điều chỉnh điểm tích lũy (kèm lý do bắt buộc), check-in vé, cập nhật trạng thái voucher.
   - Vi phạm an ninh: Mọi lần từ chối quyền (HTTP 403) và cảnh báo Audit-Only.
3. **API Truy xuất Audit Log cho Quản trị viên**:
   - `GET /api/admin/audit-logs`: Yêu cầu quyền `Audit.View`, hỗ trợ lọc theo thời gian, actor, loại hành động, mã phản hồi; hỗ trợ phân trang.

---

### 2.5. Gói 4: Cập nhật Đồng bộ Ứng dụng Web và Android (WP3)

1. **Ứng dụng Web MVC (`CinemaBD.Web`)**:
   - Cập nhật luồng Google Auth: Chuyển sang gửi Google ID Token lên `POST /api/auth/google`.
   - Cập nhật `CinemaApiClient`: Bổ sung cơ chế tự động gửi refresh token khi nhận HTTP 401.
   - Chuyển action `AccountController.Logout` sang `[HttpPost]` và gắn `[ValidateAntiForgeryToken]`.
2. **Ứng dụng Di động Android (`android/`)**:
   - Cập nhật `TokenDataStore.kt`: Nâng cấp lưu trữ JWT và Refresh Token từ plain DataStore sang `EncryptedSharedPreferences` (sử dụng Android Keystore với AES-256 GCM).
   - Cập nhật `AuthInterceptor.kt`: Triển khai OkHttp `Authenticator` tự động gọi endpoint refresh token khi API trả về mã 401 Unauthorized.

---

### 2.6. Gói 5: Kiểm thử Toàn diện & Nghiệm thu (WP6)

1. **Kiểm thử Đơn vị (Unit Tests)**:
   - Test thuật toán băm và cơ chế rehash-on-login (`Md5PasswordHasherTests`).
   - Test cơ chế lockout tài khoản và backoff thời gian (`AccountLockoutTests`).
   - Test xoay vòng refresh token và phát hiện token reuse (`RefreshTokenTests`).
   - Test `PermissionAuthorizationHandler` ở cả hai chế độ `AuditOnly = true` và `AuditOnly = false`.
2. **Kiểm thử Tích hợp (Integration Tests - WebApplicationFactory)**:
   - **`PermissionsMatrixTests`**: Tự động duyệt qua 100% action của tất cả controller trong API. Báo lỗi ngay lập tức nếu phát hiện bất kỳ action nào thiếu attribute (không có `[AllowAnonymous]` và không có `[HasPermission]`).
   - **`IdorSecurityTests`**: Kiểm thử các kịch bản User A gọi `hold-seats`, `release-seats`, `checkout`, `refund-request` với vé/hóa đơn của User B -> Đảm bảo bị từ chối 100%.
   - **`AuditLogVerificationTests`**: Đảm bảo mọi thao tác quan trọng đều sinh bản ghi trong bảng `AuditLogs`.

---

## 3. Ước lượng Tác động Hệ thống & Quản lý Rủi ro

| Gói Công việc | Số file ảnh hưởng dự kiến | Yêu cầu EF Core Migration | Mức độ Rủi ro | Giải pháp Giảm thiểu Rủi ro (Mitigation) |
|---|---|---|---|---|
| **Quick-Wins** | ~5 files (`AdminAuthApiService`, `AuthService`, `Program.cs`, `AdminAuthController`, `AdminAccountController`) | Không | Thấp | Kiểm tra unit test luồng login/logout trước khi commit. |
| **Gói 1: Runtime Permission & FallbackPolicy** | ~25 files (19 Admin Controllers, Program.cs, Authorization Handlers, Constants) | Seed data cho `ChucNang`, `PhanQuyen` | Trung bình | **Sử dụng chế độ Audit-Only (Dry-run)** trong giai đoạn đầu; **Seed role SuperAdmin** bypass để không khóa admin đang dùng. |
| **Gói 2: Quản lý Phiên & Lockout** | ~10 files (`AppDbContext`, `Entities`, `AuthService`, `JwtTokenService`, Controllers) | Bảng `RefreshTokens`, cột `AccessFailedCount`, `LockoutEndUtc` | Trung bình | Đảm bảo migration có phương thức Down() rollback; duy trì thời gian chuyển tiếp cho token. |
| **Gói 3: Nhật ký Kiểm toán** | ~8 files (`AppDbContext`, `Entities/AuditLog`, `AuditLogService`, `AdminAuditController`) | Bảng `AuditLogs` | Thấp | Bảng ghi dạng append-only, chỉ ghi thêm, không khóa bảng hay ảnh hưởng transaction nghiệp vụ chính. |
| **Gói 4: Client Web & Android** | ~8 files (`CinemaApiClient`, `TokenDataStore.kt`, `AuthInterceptor.kt`, Controllers Web) | Không | Trung bình | Test độc lập trên từng client, giữ API contract tương thích ngược. |

---

## 4. Thứ tự Thực hiện Đề xuất

```text
[Bổ sung iam-baseline.md]
          |
          v
[Tạo nist-csf-mapping.md] (File này)
          |
          v
[Gói Quick-Wins] (Rehash admin, Lỗi login chung, Web cookie/headers, Admin logout)
          |
          v
[Gói 1: Runtime Permission + SuperAdmin Bypass + Audit-Only Mode + FallbackPolicy]
          |
          v
[Chạy PermissionsMatrixTests xác minh 100% Endpoint]
          |
          v
[Gói 2: Refresh Token Xoay vòng + Account Lockout + MFA]
          |
          v
[Gói 3: Bảng AuditLog & Endpoint Truy vết An ninh]
          |
          v
[Gói 4: Đồng bộ Client Web & Android]
          |
          v
[Soạn thảo tài liệu NIST_2 đến NIST_5 & Nghiệm thu]
```
