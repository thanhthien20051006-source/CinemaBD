# CinemaBD - Website đặt vé xem phim

CinemaBD là đồ án website đặt vé rạp chiếu phim được xây dựng bằng **ASP.NET Core .NET 8** theo kiến trúc nhiều lớp. Dự án gồm API backend, MVC/Razor web frontend, database SQL Server và các module quản trị rạp/phim/suất chiếu/đặt vé/thanh toán.

## 1. Phân tích nhanh đồ án

### Công nghệ
- **.NET 8 / ASP.NET Core MVC + Web API**
- **Entity Framework Core**
- **SQL Server 2022** cho dữ liệu nghiệp vụ chính
- **SQLite** cho dữ liệu local của web MVC
- **SignalR** cập nhật trạng thái ghế realtime
- **JWT** cho API auth
- Tích hợp thanh toán demo: **VNPAY/Momo**
- Gửi email vé/hoá đơn, QR/PDF ticket
- Docker Compose để chạy full hệ thống

### Cấu trúc solution
```text
CinemaBD.Api/             REST API, Swagger, auth JWT, seed SQL Server
CinemaBD.Application/     Interface/DTO/contract tầng ứng dụng
CinemaBD.Domain/          Entity, enum, model nghiệp vụ
CinemaBD.Infrastructure/  EF Core, persistence, service implementation
CinemaBD.Web/             ASP.NET Core MVC/Razor UI khách hàng + admin
CinemaBD.schema.sql       Lược đồ DDL SQL Server
CinemaBD.data.sql         Dữ liệu mẫu SQL Server
CinemaBD.sql              Script legacy tương thích ngược
docker-compose.yml        Chạy SQL Server + API + Web bằng Docker
```

### Module chính
- Khách hàng: đăng ký/đăng nhập, xem phim, lịch chiếu, đặt ghế, combo, checkout, thanh toán, nhận vé.
- Admin: dashboard, quản lý phim, phòng/ghế, suất chiếu, khách hàng, nhân viên, vai trò, combo, hoá đơn, đánh giá, thống kê.
- Realtime: trạng thái ghế qua SignalR.
- Database initializer: thao tác tạo schema, migration và seed đều bị tắt cho đến khi cho phép tường minh qua `Database:AllowAutoMigrate` cùng `Database:AllowedNames`.

## 2. Yêu cầu cài đặt

Cài trước:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Git

Không bắt buộc cài .NET SDK nếu chỉ chạy bằng Docker.

## 3. Chạy nhanh bằng Docker

### Bước 1: Clone project
```bash
git clone https://github.com/thanhthien20051006-source/CinemaBD.git
cd CinemaBD
```

### Bước 2: Tạo file môi trường
```bash
cp .env.example .env
```

Trên Windows PowerShell nếu không có `cp`:
```powershell
Copy-Item .env.example .env
```

Mẫu `.env.example` dùng placeholder. Trước khi chạy local, thay chuỗi kết nối và cấu hình guard theo đúng database phát triển ở mục 5; khi deploy thật thì dùng secrets riêng, không dùng giá trị demo.

### Bước 3: Build và chạy container
```bash
docker compose up -d --build
```

Lần đầu chạy sẽ mất vài phút vì Docker tải image .NET/SQL Server. API không tự migrate/seed mặc định; chỉ bật các cờ khởi tạo sau khi đã chọn đúng database theo mục 5.

### Bước 4: Mở website
- Web khách hàng/admin: http://localhost:7188
- API Swagger: http://localhost:5188/swagger
- SQL Server: `localhost,1433`

## 4. Lệnh Docker hay dùng

Xem log:
```bash
docker compose logs -f
```

Xem riêng log API/Web:
```bash
docker compose logs -f api
docker compose logs -f web
```

Dừng container:
```bash
docker compose down
```

Dừng và xoá luôn dữ liệu Docker volume để seed lại từ đầu:
```bash
docker compose down -v
docker compose up -d --build
```

Build lại sau khi sửa code:
```bash
docker compose up -d --build
```

## 5. Cấu hình `.env`

Các biến quan trọng:

```env
# ConnectionStrings__DefaultConnection được Docker ghép từ biến này.
LOCAL_SQL_CONNECTION=Server=host.docker.internal,1433;Database=YOUR_DEV_DATABASE;User Id=sa;Password=CHANGE_ME_LOCAL_SA_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True

# Mặc định an toàn: initializer và expiry đều tắt.
DATABASE_ALLOW_AUTO_MIGRATE=false
DATABASE_ALLOWED_NAME=YOUR_DEV_DATABASE
DATABASE_ALLOW_DEMO_DATA=false
SHOWTIME_EXPIRY_ENABLED=false

SA_PASSWORD=Your_strong_password123!
DB_NAME=YOUR_DEV_DATABASE
JWT_KEY=CHANGE_ME_TO_A_LONG_RANDOM_SECRET_32CHARS
WEB_PUBLIC_URL=http://localhost:7188
VNPAY_TMN_CODE=CHANGE_ME
VNPAY_HASH_SECRET=CHANGE_ME
GOOGLE_CLIENT_ID=CHANGE_ME
GOOGLE_CLIENT_SECRET=CHANGE_ME
SMTP_FROM_EMAIL=change-me@example.com
SMTP_PASSWORD=CHANGE_ME
```

Ghi chú:
- `ConnectionStrings__DefaultConnection` là bắt buộc; API sẽ lỗi khởi động rõ ràng nếu thiếu. Không còn database mặc định trong `appsettings.json`.
- Mọi thao tác initializer (schema, migration, bootstrap/roles và seed) chỉ chạy khi `Database__AllowAutoMigrate=true` **và** `Database__AllowedNames` có đúng tên DB trong `Initial Catalog`. Thiếu cờ hoặc tên không khớp thì chỉ log cảnh báo rồi bỏ qua. So sánh tên không phân biệt hoa/thường.
- Docker Compose lấy `LOCAL_SQL_CONNECTION`, `DATABASE_ALLOW_AUTO_MIGRATE`, `DATABASE_ALLOWED_NAME` làm phần tử `Database__AllowedNames__0`, `DATABASE_ALLOW_DEMO_DATA`, và `SHOWTIME_EXPIRY_ENABLED`. Để cho phép một database phát triển cụ thể, chỉnh đồng thời connection string và tên allowlist cho cùng DB, rồi đặt `DATABASE_ALLOW_AUTO_MIGRATE=true`; ví dụ target `CinemaBD_Dev` thì connection string phải có `Database=CinemaBD_Dev`, `DATABASE_ALLOWED_NAME=CinemaBD_Dev`, `DATABASE_ALLOW_AUTO_MIGRATE=true`. Không đặt tên allowlist là `CinemaBD` để dùng chung chung.
- Với cấu hình không phải Docker, đặt `ConnectionStrings__DefaultConnection`, `Database__AllowAutoMigrate=true`, `Database__AllowedNames__0=<tên DB chính xác>`. Có thể thêm phần tử `Database__AllowedNames__1` cho database khác; chỉ cho phép các DB đã chủ ý liệt kê.
- `ShowtimeExpiry__Enabled=true` bật job nền; mặc định `false`.
- Demo data cần bật riêng `DatabaseSeed__AllowDemoData=true` sau khi đã cho phép initializer và chỉ ở DB phát triển phù hợp.
- `SA_PASSWORD` phải đủ mạnh theo yêu cầu SQL Server.
- Nếu không dùng Google login/email/thanh toán thật thì có thể để `CHANGE_ME`.
- Không commit file `.env` thật lên GitHub.

## 6. Chạy local không dùng Docker

Yêu cầu:
- .NET SDK 8
- SQL Server local

Lệnh build:
```bash
dotnet restore CinemaBD.WebApi.sln
dotnet build CinemaBD.WebApi.sln
```

Chạy API:
```bash
dotnet run --project CinemaBD.Api
```

Chạy Web:
```bash
dotnet run --project CinemaBD.Web
```

Mặc định local:
- API: http://localhost:5188
- Web: http://localhost:7188 hoặc port do launchSettings cấu hình

## 7. Tài khoản và dữ liệu mẫu

Database initializer chỉ chạy khi có chuỗi kết nối hợp lệ, `Database:AllowAutoMigrate=true` và `Database:AllowedNames` liệt kê đúng database. Mặc định API không tự tạo schema, migrate hay seed; xem mục 5 để cấu hình opt-in riêng cho từng database.

- **`CinemaBD.schema.sql`**: DDL được thực thi khi initializer được cho phép và database hoàn toàn trống.
- **`CinemaBD.data.sql` / `CinemaBD.sql`**: Dữ liệu mẫu chỉ nạp khi initializer đã được cho phép và `DatabaseSeed:AllowDemoData=true`.
- **Production:** giữ `DatabaseSeed:AllowDemoData=false`; bootstrap admin (nếu cần) chỉ được tạo bởi initializer sau khi cho phép database đích. Thiết lập `BOOTSTRAP_ADMIN_USERNAME` và `BOOTSTRAP_ADMIN_PASSWORD` qua secret manager hoặc biến môi trường, không ghi giá trị thật vào file cấu hình/commit.
- **Sau khi khởi tạo bootstrap:** gỡ secret bootstrap khỏi môi trường triển khai và dùng quy trình đổi mật khẩu đã được xác minh.

Không bật seed demo hoặc expiry trên production trừ khi có phê duyệt và mục đích vận hành rõ ràng.

Nếu cần dữ liệu admin/user demo trong môi trường cô lập, chủ động cho phép đúng database phát triển như mục 5 và bật `DatabaseSeed__AllowDemoData=true`; không sử dụng tài khoản mẫu trên môi trường thật.

## 8. Lỗi thường gặp

### SQL Server chưa sẵn sàng
Chạy:
```bash
docker compose logs -f sqlserver
```
Sau đó chờ thêm 1-2 phút rồi refresh web/API.

### Muốn seed lại database
```bash
docker compose down -v
docker compose up -d --build
```

### Port bị trùng
Sửa port trong `docker-compose.yml`, ví dụ:
```yaml
ports:
  - "8080:8090"
```

### Web không gọi được API
Kiểm tra biến trong container web:
```env
ApiSettings__BaseUrl=http://api:8080/
```
Không dùng `localhost` giữa các container.

## 9. Ghi chú bảo mật

Repo chỉ nên dùng key demo/placeholders. Các secret thật như SMTP password, Google Client Secret, payment secret phải để trong `.env` hoặc secret manager, không commit lên GitHub.
