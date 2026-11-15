# B3b IAM runtime status

## Scope and safety

B3b implements runtime session/token features on top of the B3 schema. The device metadata migration is present as code only and has **not** been applied. No database access, `dotnet ef database update`, or `dotnet run` was used in this change set. The full unfiltered solution test command was not run because a test can start Web/API hosts with database initialization; only explicit selected InMemory/unit test filters were executed. B4 is out of scope.

## Access JWT and sessions

- Normal access JWT lifetime: 15 minutes; JWT carries a GUID `sid` claim.
- Every authenticated API request checks the `IdentitySessions` row for a matching, unrevoked, unexpired session (and retains the existing JTI revocation check); the decision is not cached.
- Missing, duplicate, or malformed `sid` is rejected. Existing 12-hour tokens without `sid` therefore need a fresh login.
- Successful password login, registration, and Google login create a session; customer account type is `KHACHHANG`, admin type is `Admin`.
- Session listing/revocation endpoints are account-scoped. Logout revokes current session and refresh credentials; password changes revoke sessions for that account.
- `IdentitySessions.DeviceName` (100 chars) and `UserAgent` (512 chars) are bounded, untrusted display metadata. Migration `20261113110000_B3bSessionDeviceMetadata` is generated only; not applied.

## Refresh tokens

- Refresh values use 32 random bytes; storage contains only the SHA-256 digest.
- Rotation uses one conditional SQL `UPDATE` matching a still-unconsumed, unrevoked and unexpired credential/session. It requires exactly one affected row; replacement insert is committed in the same relational transaction.
- A consumed-token reuse revokes the complete session and records an audit event without raw token/hash material.
- InMemory tests cover service outcomes, but do not prove SQL Server atomicity, lock behavior, transaction behavior, or affected-row behavior under concurrency. Those remain unverified because database testing was explicitly prohibited.

## Admin permission claims

Admin login claims are sourced from current database relations: `Admin.MaCV` → active `ChucVu` (`IsMaster`) → assigned `PhanQuyen` permission names. The API keeps both a claim-based fail-closed permission filter and the existing current-database permission check, so current role changes take effect for protected admin routes rather than relying on stale JWT grants alone.

## Client compatibility

Auth response envelopes preserve existing access-token fields and add `refreshToken` and `sessionId`. Android and MVC Web clients now pass/display bounded device labels, store refresh credentials, retry a 401 once via refresh where wired, and call server logout. Legacy JWTs still require login again.

## Account Lockout (In-Memory — Trạng thái: "Một phần")

- Chính sách khóa tài khoản tạm thời được triển khai qua `AccountLockoutService` (mặc định sau 5 lần xác thực thất bại liên tiếp, thời gian khóa 15 phút, hỗ trợ thời gian khóa lũy tiến và tự động mở khóa sau khi hết hạn hoặc khi đăng nhập thành công).
- Trạng thái kiểm soát an toàn thông tin theo khung **NIST CSF 1.1 (mục PR.AC-7)** hiện được ghi nhận là **"Một phần" (Partial)**:
  - **Lý do / Giới hạn:** Dữ liệu theo dõi số lần thử sai và thời hạn khóa hiện được lưu trữ cục bộ trong bộ nhớ tiến trình (in-memory qua `ConcurrentDictionary`). Toàn bộ trạng thái khóa sẽ bị mất khi ứng dụng khởi động lại (restart) và không thể chia sẻ/đồng bộ giữa các instance API khi chạy trong môi trường phân tán hoặc cụm cân bằng tải (load balancing).
  - **Lộ trình nâng cấp đầy đủ:** Cần chuyển đổi cơ chế lưu trữ sang dạng bền vững (durable storage), hoặc lưu trữ trực tiếp vào các cột `AccessFailedCount` và `LockoutEndUtc` trong bảng CSDL theo đề xuất `CinemaBD.Infrastructure/Persistence/AccountLockoutSchemaProposal.md`, hoặc sử dụng kho dữ liệu phân tán Redis có cấu hình TTL tương ứng.

## Multi-Factor Authentication (MFA TOTP cho Admin — Mốc B5)

### 1. Kiến trúc & Chuẩn Kỹ thuật
- **Chuẩn triển khai:** RFC 6238 (TOTP) và RFC 4226 (HOTP), thuật toán băm HMAC-SHA1, độ dài mã 6 chữ số, chu kỳ bước thời gian 30 giây ($X = 30s$).
- **Bảo vệ Secret:** Secret ngẫu nhiên 160-bit (20 bytes), mã hóa Base32 cho URI `otpauth://`, được mã hóa khi lưu trữ trong CSDL qua ASP.NET Core Data Protection (`IDataProtector`), tuyệt đối không lưu khóa bản rõ.
- **Cửa sổ chấp nhận & Chống Replay:** Cho phép lệch $\pm 1$ bước thời gian ($T-1, T, T+1$, tương đương 90 giây). Tránh tấn công phát lại (Replay Attack) bằng cách lưu trữ nguyên tử `LastUsedStep` trong bảng `AdminMfa`. Mọi mã OTP thuộc bước $\le \text{LastUsedStep}$ đều bị từ chối dứt khoát.
- **So sánh hằng thời gian:** Mọi thao tác so sánh mã OTP và mã khôi phục đều sử dụng `CryptographicOperations.FixedTimeEquals` nhằm triệt tiêu nguy cơ tấn công kênh bên (timing attacks).
- **Mã Khôi phục (Recovery Codes):** Sinh 10 mã sử dụng một lần (định dạng `XXXX-XXXX`). Hệ thống chỉ lưu băm SHA-256 (`binary(32)`) trong bảng `AdminRecoveryCodes`. Mã bản rõ chỉ hiển thị duy nhất một lần khi hoàn tất enrollment. Khi sử dụng thành công, mã được đánh dấu `UsedAtUtc = DateTime.UtcNow` và không thể tái sử dụng.

### 2. Quy trình Đăng nhập 2 Bước (2-Step Authentication Flow)
1. **Bước 1 — Mật khẩu (`POST /api/admin/auth/login`):**
   - Xác thực username & password qua PBKDF2 và kiểm tra khóa tài khoản `AccountLockoutService`.
   - Nếu tài khoản đã bật MFA: API **không** phát hành Access Token, **không** phát hành Refresh Token, **không** tạo `IdentitySession`.
   - API trả về mã định danh thách thức ngắn hạn `mfa_challenge` (JWT thời hạn 5 phút, `token_type = "mfa_challenge"`, không có claim `sid`). Token này chỉ dùng duy nhất một lần cho bước xác minh và bị các endpoint nghiệp vụ khác từ chối do thiếu `sid`.
2. **Bước 2 — Xác minh OTP / Recovery Code (`POST /api/admin/auth/mfa/verify`):**
   - Client gửi `mfaChallenge`, mã xác minh `code`, và cờ `isRecoveryCode`.
   - Hệ thống xác thực token thách thức, kiểm tra và thu hồi ngay `jti` qua `RevokedTokens` để đảm bảo challenge chỉ dùng đúng một lần.
   - Xác thực mã TOTP hoặc mã khôi phục.
   - **Chống Brute Force:** Mọi lần nhập sai mã hoặc gửi challenge hết hạn đều ghi nhận đếm lỗi vào `AccountLockoutService` theo username và được bảo vệ bởi bộ giới hạn tần suất `AuthAccountRateLimitFilter`. Thông báo lỗi trả về được đồng nhất: *"Mã xác minh không chính xác hoặc phiên đã hết hạn."*
   - Khi xác minh thành công: Xóa đếm lỗi lockout, tạo `IdentitySession`, phát hành Access Token 15 phút và Refresh Token hoàn chỉnh.

### 3. Chính sách Bắt buộc cho Master Admin (`Mfa:RequireForMaster`)
- Cấu hình: `Mfa:RequireForMaster` (mặc định `true`).
- **Ràng buộc:** Bất kỳ tài khoản Admin nào mang chức vụ Master (`ChucVu.IsMaster == true`) mà chưa kích hoạt MFA:
  - Khi đăng nhập thành công bằng mật khẩu (hoặc sau khi đã đổi mật khẩu bootstrap), Access Token được cấp phát sẽ bị gắn claim giới hạn phạm vi: `mfa_enrollment_only = "true"`.
  - Bộ lọc bảo vệ `MfaEnrollmentRestrictionFilter` sẽ chặn toàn bộ các endpoint nghiệp vụ và quản trị (trả về HTTP 403 Forbidden). Master chỉ được phép gọi các endpoint thiết lập MFA (`/api/admin/auth/mfa/enroll/*`), đăng xuất, tra cứu phiên và đổi mật khẩu.
  - Master **không được phép tự tắt MFA** thông qua endpoint `POST /api/admin/auth/mfa/disable` khi chính sách `Mfa:RequireForMaster` đang bật.

### 3.1. Phân tích Rủi ro Tự Khóa (Self-Lockout Risks) & Biện pháp Phòng vệ
Khi áp dụng chính sách bắt buộc MFA nghiêm ngặt (`Mfa:RequireForMaster = true`), hệ thống nhận diện và áp dụng các biện pháp kiểm soát chống tự khóa sau:
1. **Rủi ro Deadlock giữa Đổi mật khẩu lần đầu và Thiết lập MFA**:
   - *Nguy cơ:* Tài khoản Master mới khởi tạo (hoặc vừa qua emergency recovery) đồng thời có cờ `MustChangePassword = true` và `mfa_enrollment_only = true`. Nếu hai bộ lọc chặn chéo nhau, Master không thể thao tác bất kỳ endpoint nào.
   - *Kiểm soát phòng vệ:* `MfaEnrollmentRestrictionFilter` cho phép tường minh route `POST /api/admin/auth/password/change`. Master đổi mật khẩu trước thành công -> phiên đổi mật khẩu bị thu hồi -> Master đăng nhập lại bằng mật khẩu mới -> token lúc này chỉ còn claim `mfa_enrollment_only` -> gọi `mfa/enroll/start` và `confirm` để hoàn tất kích hoạt MFA. Quy trình diễn ra tuần tự, triệt tiêu nguy cơ deadlock.
2. **Rủi ro Mất thiết bị và thất lạc toàn bộ 10 mã khôi phục**:
   - *Nguy cơ:* Vì chính sách cấm Master tự tắt MFA qua endpoint thông thường, Master sẽ bị kẹt vĩnh viễn ở Bước 2 (`/api/admin/auth/mfa/verify`).
   - *Kiểm soát phòng vệ:* Thiết lập cơ chế cứu hộ khẩn cấp (Break-Glass) độc lập thông qua `IAdminEmergencyRecoveryService` sử dụng khóa bí mật vận hành `ADMIN_RECOVERY_KEY`. Cơ chế này chỉ chạy ở tầng kỹ thuật/vận hành nội bộ (không mở API public), xóa toàn bộ MFA cũ, đặt lại mật khẩu tạm thời và ghi audit log mức nghiêm trọng.
3. **Rủi ro Mất khóa giải mã Data Protection (Key Ring Disruption)**:
   - *Nguy cơ:* Nếu Data Protection key ring dùng bộ nhớ tạm (in-memory) và server bị restart, toàn bộ TOTP secret (`SecretProtected`) trong CSDL không thể giải mã (`CryptographicException`), gây khóa toàn bộ tài khoản Admin.
   - *Kiểm soát phòng vệ:* Đã cấu hình `PersistKeysToFileSystem` với thư mục lưu trữ bền vững (hoặc persistent volume trong Docker) và gán `SetApplicationName("CinemaBD")`.
4. **Rủi ro Tấn công Brute-Force gây DoS Lockout**:
   - *Nguy cơ:* Kẻ tấn công cố tình nhập sai OTP để kích hoạt `AccountLockoutService` khóa tài khoản Master 15 phút.
   - *Kiểm soát phòng vệ:* Kết hợp bộ lọc giới hạn tần suất theo IP (`AuthAccountRateLimitFilter`, `AuthRateLimitFilter`) để chặn nguồn tấn công trước khi đạt ngưỡng khóa tài khoản; trạng thái lockout tự động giải phóng khi hết hạn hoặc khi kích hoạt quy trình cứu hộ khẩn cấp.

### 4. Quy trình Khôi phục Sự cố Khẩn cấp Master (Emergency Master Recovery Runbook)
Trong tình huống Master admin bị mất thiết bị xác thực và làm mất toàn bộ 10 mã khôi phục:
- **Nguyên tắc vận hành:** Không cung cấp endpoint HTTP công khai tự động để tránh nguy cơ bypass xác thực. Quy trình khôi phục được kiểm soát kép (Dual-Control) thông qua dịch vụ kỹ thuật `IAdminEmergencyRecoveryService` (`EmergencyResetMasterMfaAsync`).
- **Điều kiện thực thi:**
  1. Yêu cầu khóa cứu hộ khẩn cấp `ADMIN_RECOVERY_KEY` (hoặc cấu hình `Security:AdminRecoveryKey`), tách biệt hoàn toàn với `BOOTSTRAP_ADMIN_PASSWORD` chỉ dùng cho seed CSDL ban đầu.
  2. Xác thực kiểm tra hằng thời gian chuỗi bí mật khôi phục (`CryptographicOperations.FixedTimeEquals`).
- **Hành động xử lý khi khôi phục thành công:**
  1. Hủy bỏ bản ghi `AdminMfa` và xóa toàn bộ mã khôi phục cũ của tài khoản Master.
  2. Đặt cờ `MustChangePassword = true` và `PasswordExpiresAt = DateTime.UtcNow.AddHours(24)`.
  3. Thu hồi toàn bộ các phiên hoạt động hiện tại của tài khoản Master (`_identitySessionService.RevokeAllAsync`).
  4. Ghi nhận nhật ký kiểm toán an ninh mức nghiêm trọng (`EventType: "emergency_mfa_reset"`).
  5. Yêu cầu Master admin đăng nhập lại, đổi mật khẩu mới và tiến hành thiết lập lại thiết bị MFA TOTP ngay lập tức.

