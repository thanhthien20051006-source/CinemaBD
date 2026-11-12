# CinemaBD — Hướng dẫn làm việc trong repository

## Tổng quan

CinemaBD là hệ thống đặt vé xem phim gồm backend/API ASP.NET Core .NET 8, website MVC/Razor, cơ sở dữ liệu nghiệp vụ SQL Server và ứng dụng Android Kotlin. Các luồng sản phẩm chính gồm xem phim/lịch chiếu, chọn ghế, đặt vé, thanh toán, nhận vé; phần quản trị có quản lý phim, suất chiếu, rạp/phòng/ghế, khách hàng/nhân viên, hóa đơn, voucher và thống kê.

Trước khi thay đổi, đọc `README.md` để biết cách chạy/cấu hình và tài liệu liên quan trong `docs/`. `CinemaBD/` là mã MVC 5 cũ để tham khảo/migration; solution .NET 8 hiện hành là `CinemaBD.WebApi.sln`.

## Bản đồ cấu trúc

```text
CinemaBD.WebApi.sln
├── CinemaBD.Api/             REST API: controllers, contracts, middleware, JWT, Swagger
├── CinemaBD.Application/     Interface và contract của nghiệp vụ
├── CinemaBD.Domain/          Entity, enum và mô hình nghiệp vụ lõi
├── CinemaBD.Infrastructure/  EF Core, persistence, migrations, bảo mật, tích hợp và service
└── CinemaBD.Web/             Giao diện MVC/Razor khách hàng và admin, API client, SignalR

android/                      Ứng dụng Android Kotlin/Jetpack Compose
  └── CLAUDE.md               Hướng dẫn riêng cho Android

docs/                         Tài liệu kiến trúc, kế hoạch và ghi chú API
CinemaBD.sql                  Script dữ liệu SQL Server
.env.example                  Mẫu biến môi trường; chỉ dùng placeholder
Dockerfile*, docker-compose.yml  Cấu hình container (nếu có trong checkout)
```

Entrypoints tham khảo:

- API: `CinemaBD.Api/Program.cs` — DI, JWT, authorization, exception handling, Swagger và khởi tạo database.
- Web: `CinemaBD.Web/Program.cs` — MVC, session/authentication, API clients và SignalR.
- Android: `android/app/src/main/java/com/cinemabd/app/MainActivity.kt` và `android/app/src/main/java/com/cinemabd/app/navigation/CinemaBdNavHost.kt`.

## Kiến trúc và quy ước thay đổi

Tham khảo `docs/LAYERED_ARCHITECTURE.md` khi thay đổi backend:

- `Domain` giữ mô hình nghiệp vụ lõi, không phụ thuộc framework/hạ tầng.
- `Application` định nghĩa interface/contract và phụ thuộc `Domain`.
- `Infrastructure` triển khai service, persistence và tích hợp kỹ thuật; phụ thuộc `Application` và `Domain`.
- `Api` nhận/trả HTTP, gọi nghiệp vụ qua interface và đăng ký hạ tầng qua DI. Giữ controller mỏng; không đặt truy cập DB hay nghiệp vụ phức tạp trong controller.
- `Web` là frontend MVC riêng. Luồng chính gọi API qua HTTP/`CinemaApiClient`, nhưng cần kiểm tra từng feature vì một số phần còn dùng dịch vụ hoặc dữ liệu local.
- API contracts nằm trong `CinemaBD.Api/Contracts/`; khi đổi request/response hoặc hành vi endpoint, kiểm tra các consumer liên quan trong Web và Android cùng tài liệu API.

Với thay đổi Android, đọc `android/CLAUDE.md` và tài liệu API thích hợp như `docs/ANDROID_API_NOTES.md` hoặc `docs/ANDROID_ADMIN_API_NOTES.md` trước khi sửa.

## Mục tiêu Bảo mật (NIST CSF 1.1)

Dự án này đang được nâng cấp để phục vụ đồ án: **“Xây dựng hệ thống bảo vệ và giám sát an toàn thông tin cho ứng dụng đặt vé xem phim theo khung NIST CSF 1.1”.**

Mọi thay đổi về code Backend và API phải được xem xét theo năm trụ cột sau:

1. **Identify (Kiểm kê & Đánh giá rủi ro):** xác định tài sản, dữ liệu, thành phần và rủi ro liên quan đến thay đổi.
2. **Protect (Bảo vệ):** bảo vệ xác thực và phân quyền; chống gian lận đặt vé; ký số QR để hỗ trợ xác minh tính toàn vẹn/xác thực của vé.
3. **Detect (Phát hiện):** ghi log chuẩn hóa, đủ ngữ cảnh cần thiết và phát hiện hành vi bất thường.
4. **Respond (Ứng phó):** hỗ trợ khóa IP/token tự động khi có sự cố; thiết kế ngưỡng, thời hạn, audit và cơ chế mở khóa/khôi phục an toàn để hạn chế khóa nhầm.
5. **Recover (Khôi phục):** hỗ trợ sao lưu và phục hồi cơ sở dữ liệu; cần tính đến kiểm tra khả năng phục hồi và tính toàn vẹn dữ liệu.

Đây là mục tiêu/định hướng bảo mật của đồ án, không phải khẳng định rằng mọi kiểm soát trên đã được triển khai. Trước khi mô tả một kiểm soát là hiện hữu, hãy xác minh trong code và cấu hình liên quan; phân biệt rõ trạng thái đã có, còn thiếu và đề xuất. Không ghi log mật khẩu, token, khóa bí mật hoặc dữ liệu nhạy cảm không cần thiết.

## Cấu hình và kiểm chứng

- Cấu hình khởi tạo CSDL: `DatabaseSeed:AllowDemoData` mặc định là `true` trong Development (`appsettings.Development.json`) để tự động tạo dữ liệu rạp/phim/suất chiếu demo; trong Production mặc định là `false` để ngăn chặn lộ tài khoản demo. Khi CSDL trống, schema DDL (`CinemaBD.schema.sql`) luôn luôn được thực thi; Roles và Permissions luôn được seed độc lập với cờ demo. Tạo tài khoản quản trị ban đầu trên CSDL production sạch thông qua biến môi trường `BOOTSTRAP_ADMIN_USERNAME` và `BOOTSTRAP_ADMIN_PASSWORD` (tuyệt đối không log mật khẩu này; tài khoản bắt buộc đổi mật khẩu lần đầu trong 7 ngày).
- Không đưa secret thật vào source, tài liệu, log hoặc commit. Dùng biến môi trường/secret manager theo cấu hình triển khai; chỉ dùng placeholder trong `.env.example`. Không sao chép giá trị nhạy cảm từ `appsettings*.json` vào tài liệu.
- Giữ đúng ranh giới phân tầng và tái sử dụng service/interface hiện có trước khi thêm cơ chế mới. Khi sửa API, rà soát authorization, validation, response contract, logging và các client bị ảnh hưởng.
- Dùng các lệnh build/run và Docker đã ghi trong `README.md`; chỉ báo cáo kiểm thử/build thực sự đã chạy và kết quả.
- Chỉ thay đổi những phần cần thiết cho yêu cầu hiện tại; không ghi đè các thay đổi chưa commit khác trong working tree.
