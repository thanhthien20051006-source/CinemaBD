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
