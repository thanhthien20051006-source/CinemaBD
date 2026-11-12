# PROMPT CHO CLAUDE CODE: HỆ THỐNG QUẢN LÝ XÁC THỰC VÀ PHÂN QUYỀN (IAM) THEO NIST CSF 1.1

> File này đặt tại `docs\nist-iam-prompt.md`. Chạy Claude Code ở thư mục gốc `D:\ALL_CNTT\CinemaBD`.

## 0. Vai trò và mục tiêu

Bạn là kỹ sư bảo mật phần mềm cấp cao. Hãy xây dựng và củng cố hệ thống **quản lý xác thực (Authentication) và phân quyền truy cập (Authorization)** cho nền tảng đặt vé xem phim **CinemaBD**, bám theo khung **NIST Cybersecurity Framework 1.1** (5 chức năng: Identify, Protect, Detect, Respond, Recover). Trọng tâm là nhóm **PR.AC** (Access Control), kèm các nhóm liên quan: PR.DS, PR.PT, PR.IP, DE.AE, DE.CM, RS, RC, ID.

## 1. Bối cảnh dự án (đã khảo sát, hãy tự xác minh lại bằng code)

- Solution `CinemaBD.WebApi.sln`, .NET 8, kiến trúc nhiều tầng: `CinemaBD.Domain`, `CinemaBD.Application`, `CinemaBD.Infrastructure`, `CinemaBD.Api` (Web API), `CinemaBD.Web` (web khách hàng), thư mục `android\` (app Kotlin, Gradle).
- CSDL SQL Server, EF Core 8.0.12 (có thư mục `Infrastructure\Migrations`), file `CinemaBD.sql` được copy vào Docker image. Chạy bằng `docker-compose.yml`.
- Xác thực hiện có: `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.12; `AuthController` (login, register, google, logout); `AdminAuthController` (login riêng cho admin).
- Phân quyền hiện có: `AdminRolesController` (CRUD role, danh sách permission, gán/gỡ permission cho role), `AdminEmployeesController`, `AdminCustomersController`.
- Nhóm endpoint nghiệp vụ nhạy cảm: Admin* (movies, showtimes, vouchers, refunds, loyalty points, invoices/check-in, statistics...), `BookingsController` (hold-seats, checkout, refund-request), `AccountController`, `AccountTicketsController`, `PaymentsController`.
- Tích hợp ngoài: Google OAuth, VNPay, MoMo, SMTP. Biến môi trường chính: `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`, `QR_SIGNING_KEY`, `CORS_ALLOWED_ORIGIN`, `GOOGLE_*`, `VNPAY_*`, `MOMO_*`, `SMTP_*`.
- Có sẵn các file hướng dẫn: `CLAUDE.md`, `AGENTS.md`, `android\CLAUDE.md`, `audit_actions.txt`, `README_TEST_API.md`, `CinemaBD.PostmanCollection.json`. **Đọc chúng trước tiên và tuân theo quy ước trong đó.**
- Thư mục `docs\` đã có tài liệu dự án, trong đó **`NIST_1_IDENTIFY.md` (chức năng Identify đã được làm trước đó)**. Ngoài ra có `THIET_KE_KIEN_TRUC_PHAN_TANG.md`, `LAYERED_ARCHITECTURE.md`, `BAO_CAO_DO_AN_CINEMABD_4_CHUO...md`, `KE_HOACH_PHAT_TRIEN_DU_AN.md`, `ANDROID_API_NOTES.md`, `ANDROID_ADMIN_API_NOTES.md`. **Không làm lại phần Identify**: đọc kỹ `NIST_1_IDENTIFY.md`, kế thừa cách đánh mã tài sản/rủi ro, thuật ngữ và định dạng, chỉ bổ sung những gì còn thiếu cho IAM.
- Nhánh hiện tại là `feature/waf` (commit cuối: "wip: luu trang thai truoc khi lam WAF"). Không làm lẫn công việc WAF vào IAM.

### Giả định (người dùng chưa xác nhận, nếu sai sẽ báo lại)

- Vai trò: Khách hàng (Customer) và các vai trò nội bộ được quản lý động qua bảng Role/Permission (ví dụ: SuperAdmin, Quản lý rạp, Nhân viên soát vé, Biên tập nội dung, Kế toán).
- MFA (TOTP) **bắt buộc** cho tài khoản admin/nhân viên, **tùy chọn** cho khách hàng.
- Phạm vi NIST: tập trung Protect và Detect, kèm tài liệu ánh xạ và runbook ngắn cho Respond/Recover.
- Có cả client Web và Android dùng chung API, nên mọi thay đổi hợp đồng API phải cập nhật cả hai.

## 2. Quy tắc làm việc (bắt buộc)

1. **Không bao giờ in, ghi log, commit hoặc sửa** giá trị trong `.env`, `certs\*.pfx`, User Secrets, hoặc bất kỳ secret nào. Chỉ thao tác với tên biến và `.env.example`.
2. Tạo nhánh mới `feature/nist-iam` (hỏi tôi nên rẽ từ `master` hay `feature/waf` nếu chưa rõ). Commit nhỏ theo từng gói công việc, message tiếng Việt không dấu theo kiểu hiện tại (`feat(api): ...`, `fix(...)`).
3. Sau mỗi gói công việc: `dotnet build CinemaBD.WebApi.sln` và `dotnet test` phải qua. Nếu chưa có test project thì tạo `CinemaBD.Tests`.
4. Mọi thay đổi schema đi qua **EF Core migration** có thể rollback. Không chạy lệnh phá hủy dữ liệu (drop database, xóa bảng). Không sửa `CinemaBD.sql` trừ khi cần thiết và phải nêu rõ.
5. Giữ **tương thích ngược** cho Web và Android. Nếu đổi hợp đồng API (ví dụ thêm refresh token), cập nhật luôn client trong `CinemaBD.Web` và `android\`, `CinemaBD.PostmanCollection.json`, `README_TEST_API.md`.
6. Thông báo lỗi trả về người dùng bằng tiếng Việt, **không lộ chi tiết nội bộ** và **không cho phép dò tài khoản** (thông báo đăng nhập sai phải chung chung).
7. Làm theo từng Phase. **Dừng lại và chờ tôi duyệt** ở cuối Phase 0 và Phase 1. Dùng Plan Mode cho hai phase này.
8. Không thêm thư viện mới nếu .NET 8 đã có sẵn giải pháp. Nếu cần (TOTP, Argon2...), nêu lý do và phiên bản trước khi thêm.

## 3. Phase 0: Khảo sát hiện trạng (chỉ đọc, không sửa code)

Trước hết đọc `docs\NIST_1_IDENTIFY.md` và các tài liệu kiến trúc trong `docs\`, ghi lại những gì đã có (tài sản, rủi ro, quyết định thiết kế) để không lặp lại.

Sau đó tạo `docs\security\iam-baseline.md` gồm:

1. Luồng đăng nhập/đăng ký/Google/logout/admin login hiện tại, vẽ bằng sơ đồ văn bản.
2. Cấu hình JWT: thuật toán, thời hạn token, kiểm tra issuer/audience/lifetime, ClockSkew, độ dài key, nơi lưu key.
3. Lưu trữ mật khẩu: thuật toán băm, salt, có cơ chế nâng cấp hash không.
4. **Ma trận phủ quyền**: liệt kê **mọi endpoint** của `CinemaBD.Api` với các cột: route, HTTP method, `[Authorize]`/`[AllowAnonymous]`, role/policy/permission yêu cầu, có kiểm tra quyền sở hữu dữ liệu (chống IDOR) hay không. Đánh dấu rõ endpoint **thiếu bảo vệ**.
5. Cơ chế permission hiện có của `AdminRolesController`: permission được kiểm tra ở đâu khi gọi API admin? Có thật sự được thực thi hay chỉ lưu trong DB?
6. Quản lý phiên: logout có thu hồi token thật không (JWT stateless), refresh token, thời hạn.
7. CORS, rate limiting, HTTPS/HSTS, security headers, cookie của `CinemaBD.Web`, cách app Android lưu token.
8. Log: có ghi mật khẩu/token/PII không? Đọc `audit_actions.txt` và `ExceptionHandlingMiddleware`. Có audit log nghiệp vụ trong DB chưa?
9. Vệ sinh repo: dùng `git ls-files` kiểm tra `.env`, `certs\*.pfx`, `*.log`, `hs_err_pid*.log`, `local.properties`, `.vs`, `.idea` có bị theo dõi bởi git không. Chỉ báo cáo, không tự xóa khỏi lịch sử.
10. Xác thực callback thanh toán VNPay/MoMo (chữ ký, idempotency): chỉ ghi nhận hiện trạng.

**Kết thúc Phase 0 bằng bản tóm tắt 15 dòng và chờ tôi duyệt.**

## 4. Phase 1: Phân tích khoảng cách theo NIST CSF 1.1 và kế hoạch

Phần Identify đã có trong `docs\NIST_1_IDENTIFY.md`: chỉ **tham chiếu và bổ sung** các subcategory còn thiếu cho IAM, không viết lại. Các tài liệu NIST sau này đặt tên cùng quy ước: `docs\NIST_2_PROTECT.md`, `docs\NIST_3_DETECT.md`, `docs\NIST_4_RESPOND.md`, `docs\NIST_5_RECOVER.md`.

Tạo `docs\security\nist-csf-mapping.md` (bảng tổng hợp, liên kết tới các file NIST_* ở trên) với bảng: `Function | Category/Subcategory | Kiểm soát cần có | Hiện trạng | Khoảng cách | Gói công việc | Bằng chứng (file/test)`. Tối thiểu bao gồm:

- **Identify:** ID.AM-2 (kiểm kê thành phần phần mềm/API/endpoint), ID.RA-1 (lỗ hổng đã nhận diện, lấy từ Phase 0), ID.GV-1 (chính sách IAM).
- **Protect:** PR.AC-1 (cấp phát, quản lý, thu hồi, kiểm toán định danh và thông tin xác thực), PR.AC-3 (truy cập từ xa), PR.AC-4 (quyền tối thiểu, tách biệt nhiệm vụ), PR.AC-6 (định danh gắn với thông tin xác thực), PR.AC-7 (mức xác thực tương xứng rủi ro), PR.DS-1/2/5 (dữ liệu lưu trữ, truyền tải, chống rò rỉ), PR.IP-1 (cấu hình nền), PR.PT-1 (nhật ký kiểm toán), PR.PT-3 (chức năng tối thiểu).
- **Detect:** DE.AE-2, DE.AE-3, DE.CM-3, DE.CM-7.
- **Respond/Recover:** RS.AN-1, RS.MI-2, RS.RP-1, RC.RP-1.

Sau bảng, đề xuất **kế hoạch triển khai** theo các gói ở Phase 2 (ước lượng file ảnh hưởng, migration, rủi ro, thứ tự). **Dừng lại chờ tôi duyệt.**

## 5. Phase 2: Triển khai theo gói (mỗi gói = 1+ commit, có test)

### WP1: Xác thực (PR.AC-1, PR.AC-6, PR.AC-7)
- Chính sách mật khẩu: độ dài tối thiểu, chặn mật khẩu phổ biến/đã lộ (danh sách cục bộ), không bắt buộc ký tự đặc biệt cứng nhắc.
- Băm mật khẩu bằng thuật toán hiện đại (Argon2id hoặc PBKDF2/BCrypt với cost hợp lý). **Nâng cấp hash cũ khi người dùng đăng nhập thành công** (rehash-on-login), không ép đặt lại hàng loạt.
- Khóa tài khoản tạm thời + backoff khi sai nhiều lần; đếm theo tài khoản **và** theo IP.
- Access token ngắn hạn (10-15 phút) + **refresh token xoay vòng** lưu **dạng băm** trong DB, phát hiện tái sử dụng (reuse detection) thì thu hồi cả chuỗi. Logout thu hồi refresh token và vô hiệu access token còn hạn (jti denylist hoặc `TokenVersion`/`SecurityStamp`).
- Quản lý phiên: liệt kê và thu hồi phiên theo thiết bị.
- MFA TOTP: bắt buộc cho admin/nhân viên, tùy chọn cho khách; mã khôi phục dùng một lần.
- Đăng nhập Google: xác minh `id_token` (chữ ký, `aud`, `iss`, `exp`, `email_verified`); quy tắc liên kết tài khoản an toàn.
- Xác minh email khi đăng ký; quên/đặt lại mật khẩu bằng token một lần, ngắn hạn, lưu dạng băm; đổi mật khẩu thì thu hồi mọi phiên.
- Thông báo lỗi chung chung, thời gian phản hồi không để lộ tài khoản có tồn tại hay không.

### WP2: Phân quyền (PR.AC-4)
- Định nghĩa **danh mục permission** dạng hằng số (ví dụ `Movies.Manage`, `Refunds.Approve`, `Loyalty.Adjust`, `Roles.Manage`, `Invoices.CheckIn`...), đồng bộ vào DB khi khởi động (seed idempotent).
- Triển khai cơ chế `[HasPermission("...")]` bằng `IAuthorizationPolicyProvider` + `AuthorizationHandler`, đọc quyền từ DB/cache có hạn; **thay đổi role có hiệu lực ngay** (không phải chờ token hết hạn).
- **Deny-by-default:** đặt `FallbackPolicy = RequireAuthenticatedUser`; endpoint công khai phải ghi `[AllowAnonymous]` tường minh. Gắn permission cho **toàn bộ** endpoint Admin*, theo ma trận từ Phase 0.
- Chống IDOR: khách chỉ truy cập được dữ liệu của chính mình (profile, vé, lịch sử, loyalty, hold/release seats, checkout, refund-request). Kiểm tra quyền sở hữu ở tầng Application, không chỉ ở controller.
- Tách biệt nhiệm vụ và quyền tối thiểu: người dùng không tự nâng quyền hoặc tự sửa role của mình; không gán permission mà mình không có; không cho xóa/hạ quyền SuperAdmin cuối cùng; người duyệt hoàn tiền không được là người tạo yêu cầu (nếu áp dụng); điều chỉnh điểm loyalty phải có lý do bắt buộc.
- Seed bộ role mặc định theo nguyên tắc quyền tối thiểu (xem Giả định).

### WP3: Bảo vệ dữ liệu và cấu hình liên quan IAM (PR.DS, PR.IP, PR.PT-3)
- Kiểm tra khi khởi động (fail-fast): `JWT_KEY` đủ dài/không phải `CHANGE_ME`, `QR_SIGNING_KEY`, CORS chỉ cho origin cấu hình.
- HTTPS + HSTS ở production, security headers cơ bản, cookie của Web: `HttpOnly`, `Secure`, `SameSite`.
- Rate limiting (`AddRateLimiter`) cho login, register, forgot-password, MFA, hold-seats, checkout.
- Hỗ trợ **xoay khóa JWT** (`kid`, chấp nhận key cũ trong thời gian chuyển tiếp).
- Android: lưu token bằng `EncryptedSharedPreferences`/Keystore, xử lý 401/refresh thống nhất; không log token.
- Che/ẩn PII và token trong log. Chỉ **báo cáo** (không tự sửa) các vấn đề về callback thanh toán nếu phát hiện.

### WP4: Nhật ký kiểm toán và phát hiện (PR.PT-1, DE.AE-2/3, DE.CM-3/7)
- Bảng `AuditLog` chỉ ghi thêm (append-only): thời điểm UTC, actor, vai trò, hành động, tài nguyên, kết quả, IP, user-agent, correlation id. Dùng lại danh sách trong `audit_actions.txt` nếu phù hợp.
- Ghi sự kiện: đăng nhập thành công/thất bại, khóa tài khoản, MFA, đổi/đặt lại mật khẩu, tạo/sửa/xóa nhân viên và khách, **mọi thay đổi role/permission**, duyệt/từ chối hoàn tiền, điều chỉnh loyalty, thay đổi voucher, check-in vé, từ chối truy cập (403).
- Quy tắc phát hiện đơn giản: nhiều đăng nhập sai từ một IP, một tài khoản đăng nhập từ nhiều IP trong thời gian ngắn, tăng quyền bất thường, thao tác admin ngoài giờ. Cảnh báo qua email dùng cấu hình SMTP sẵn có.
- Endpoint đọc audit/security events cho admin (cần permission riêng, phân trang, lọc theo actor/hành động/thời gian).

### WP5: Respond/Recover và tài liệu (RS, RC, ID.GV-1)
- `docs\security\iam-policy.md`: chính sách IAM ngắn gọn (vòng đời tài khoản, rà soát quyền định kỳ, mật khẩu, MFA).
- `docs\security\runbook-account-compromise.md`: quy trình khi lộ tài khoản/key (thu hồi toàn bộ phiên, xoay JWT key, khóa tài khoản, khôi phục từ backup, thông báo).
- Viết `docs\NIST_2_PROTECT.md`, `docs\NIST_3_DETECT.md`, `docs\NIST_4_RESPOND.md`, `docs\NIST_5_RECOVER.md` theo định dạng của `NIST_1_IDENTIFY.md`, mỗi file mô tả kiểm soát đã triển khai và dẫn tới code/test làm bằng chứng.
- Cập nhật `nist-csf-mapping.md` cột "Bằng chứng" và trạng thái từng subcategory (Đạt / Một phần / Chưa).

### WP6: Kiểm thử (xuyên suốt)
- Unit test: băm/nâng cấp hash, lockout, refresh rotation + reuse detection, handler permission.
- Integration test (`WebApplicationFactory`): ma trận quyền **tự động duyệt toàn bộ endpoint** và fail nếu có endpoint không có policy/AllowAnonymous tường minh; test IDOR; test 401/403; test audit log được ghi.
- Cập nhật Postman collection và `README_TEST_API.md`.

## 6. Tiêu chí nghiệm thu

- Không còn endpoint nào truy cập được ngoài ý muốn; ma trận quyền khớp với thiết kế và được test tự động.
- Đăng nhập sai nhiều lần bị khóa/giãn; refresh token tái sử dụng bị thu hồi; logout vô hiệu hóa phiên thật sự.
- Thay đổi role có hiệu lực ngay và luôn để lại bản ghi audit.
- Admin/nhân viên bắt buộc MFA; khách hàng dùng Web/Android vẫn đăng nhập, đặt vé, thanh toán bình thường.
- `dotnet build`, `dotnet test` đều xanh; migration áp dụng và rollback được trên DB trống.
- `nist-csf-mapping.md` đầy đủ, mỗi subcategory có bằng chứng truy vết được tới file/test cụ thể.

## 7. Cách báo cáo

Sau mỗi phase/gói: (1) tóm tắt đã làm, (2) danh sách file thay đổi, (3) kết quả build/test, (4) rủi ro hoặc việc cần tôi quyết định, (5) bước tiếp theo đề xuất. Nếu gặp điểm mơ hồ có thể làm hỏng hành vi hiện có (ví dụ định dạng token Android đang dùng), **hỏi tôi trước khi sửa**.

**Bắt đầu ngay với Phase 0.**
