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
CinemaBD.sql              Script dữ liệu mẫu SQL Server
docker-compose.yml        Chạy SQL Server + API + Web bằng Docker
```

### Module chính
- Khách hàng: đăng ký/đăng nhập, xem phim, lịch chiếu, đặt ghế, combo, checkout, thanh toán, nhận vé.
- Admin: dashboard, quản lý phim, phòng/ghế, suất chiếu, khách hàng, nhân viên, vai trò, combo, hoá đơn, đánh giá, thống kê.
- Realtime: trạng thái ghế qua SignalR.
- Database seed: API tự import `CinemaBD.sql` khi SQL Server container chưa có bảng.

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

Có thể giữ nguyên giá trị demo trong `.env` để chạy local. Khi deploy thật thì đổi các key/password.

### Bước 3: Build và chạy container
```bash
docker compose up -d --build
```

Lần đầu chạy sẽ mất vài phút vì Docker tải image .NET/SQL Server và API import dữ liệu mẫu.

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
SA_PASSWORD=Your_strong_password123!
DB_NAME=CinemaBD
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

Dữ liệu mẫu được import từ `CinemaBD.sql` khi chạy API lần đầu với database trống. Nếu cần tài khoản admin/user, kiểm tra trong script `CinemaBD.sql` hoặc màn hình đăng nhập sau khi seed xong.

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
