# THIẾT KẾ KIẾN TRÚC PHÂN TẦNG DỰ ÁN CINEMABD

## 1. Tổng quan kiến trúc

Dự án **CinemaBD** được thiết kế theo mô hình **Layered Architecture - kiến trúc phân tầng** trên nền tảng **ASP.NET Core .NET 8**. Mục tiêu của kiến trúc này là tách biệt rõ trách nhiệm giữa các thành phần: giao diện, API, nghiệp vụ, truy cập dữ liệu và hạ tầng kỹ thuật.

Solution chính:

```text
CinemaBD.WebApi.sln
├── CinemaBD.Domain
├── CinemaBD.Application
├── CinemaBD.Infrastructure
├── CinemaBD.Api
└── CinemaBD.Web
```

Mô hình tổng thể:

```text
┌──────────────────────────────────────────────┐
│ Client / Browser                             │
└───────────────────────┬──────────────────────┘
                        │
                        v
┌──────────────────────────────────────────────┐
│ CinemaBD.Web                                 │
│ ASP.NET Core MVC/Razor UI                    │
│ Trang khách hàng + Trang quản trị            │
└───────────────────────┬──────────────────────┘
                        │ HTTP / CinemaApiClient
                        v
┌──────────────────────────────────────────────┐
│ CinemaBD.Api                                 │
│ REST API Controller + Swagger + JWT Auth     │
└───────────────────────┬──────────────────────┘
                        │ gọi interface service
                        v
┌──────────────────────────────────────────────┐
│ CinemaBD.Application                         │
│ Interface / Contract nghiệp vụ               │
└───────────────────────┬──────────────────────┘
                        │ implement bởi
                        v
┌──────────────────────────────────────────────┐
│ CinemaBD.Infrastructure                      │
│ EF Core, SQL Server, JWT, Payment, Services  │
└───────────────────────┬──────────────────────┘
                        │
                        v
┌──────────────────────────────────────────────┐
│ Database                                     │
│ SQL Server + SQLite local web                │
└──────────────────────────────────────────────┘
```

---

## 2. Nguyên tắc phụ thuộc giữa các tầng

Kiến trúc được tổ chức theo hướng dependency một chiều:

```text
CinemaBD.Web ───────────────┐
                            v
CinemaBD.Api ───────> CinemaBD.Infrastructure ───────> CinemaBD.Application ───────> CinemaBD.Domain
```

Quy tắc chính:

- `Domain` không phụ thuộc tầng nào.
- `Application` chỉ phụ thuộc `Domain`.
- `Infrastructure` phụ thuộc `Application` và `Domain` để implement interface.
- `Api` phụ thuộc `Infrastructure` để đăng ký DI và gọi service.
- `Web` là tầng giao diện, có thể gọi API hoặc dùng service local của web.
- Controller không nên xử lý nghiệp vụ phức tạp.
- Logic truy cập database không đặt trong `Application` hoặc Controller.

---

## 3. Vai trò từng tầng trong dự án

## 3.1. Tầng Domain - `CinemaBD.Domain`

### Vai trò

`CinemaBD.Domain` là tầng lõi của hệ thống, chứa các entity, enum và model nghiệp vụ độc lập với framework. Đây là tầng ổn định nhất, không phụ thuộc vào ASP.NET Core, EF Core, SQL Server hay giao diện.

### Thành phần chính

```text
CinemaBD.Domain/
├── Entities/
└── Enums/
```

Một số entity tiêu biểu:

```text
Movie.cs
Showtime.cs
Room.cs
Seat.cs
Booking.cs
Invoice.cs
Customer.cs
Employee.cs
Role.cs
Combo.cs
Review.cs
Voucher.cs
LoyaltyPoint.cs
RefundModels.cs
RevenueStatistics.cs
```

### Trách nhiệm

- Mô tả đối tượng nghiệp vụ của hệ thống rạp phim.
- Định nghĩa cấu trúc dữ liệu cốt lõi.
- Là nền tảng cho các tầng Application và Infrastructure.

### Ví dụ nghiệp vụ thuộc Domain

- Phim có mã phim, tên phim, thời lượng, trạng thái.
- Suất chiếu thuộc một phim và một phòng chiếu.
- Ghế thuộc một phòng và có trạng thái.
- Hóa đơn gắn với khách hàng, vé, combo và thanh toán.

---

## 3.2. Tầng Application - `CinemaBD.Application`

### Vai trò

`CinemaBD.Application` chứa các **interface/service contract** mô tả các chức năng nghiệp vụ mà hệ thống cần có. Tầng này không trực tiếp truy cập database, không biết EF Core, không biết SQL Server.

### Thành phần chính

```text
CinemaBD.Application/
├── Interfaces/
└── Services/
```

Một số interface chính:

```text
IAuthService.cs
IMovieService.cs
IShowtimeService.cs
ISeatService.cs
IBookingService.cs
IPaymentService.cs
IReviewService.cs
ICustomerProfileService.cs
ILoyaltyPointService.cs
```

Một số interface admin:

```text
IAdminDashboardService.cs
IAdminMovieService.cs
IAdminShowtimeService.cs
IAdminRoomService.cs
IAdminSeatService.cs
IAdminComboService.cs
IAdminInvoiceService.cs
IAdminStatisticsService.cs
IAdminVoucherService.cs
IAdminLoyaltyPointService.cs
IAdminRefundService.cs
```

### Trách nhiệm

- Định nghĩa nghiệp vụ hệ thống dưới dạng interface.
- Tách phần khai báo nghiệp vụ khỏi phần cài đặt kỹ thuật.
- Giúp API không phụ thuộc trực tiếp vào class implementation.
- Hỗ trợ kiểm thử và mở rộng.

### Ví dụ

API cần lấy danh sách phim sẽ gọi interface:

```csharp
IMovieService
```

Còn class cài đặt thật nằm ở tầng Infrastructure:

```csharp
MovieService : IMovieService
```

---

## 3.3. Tầng Infrastructure - `CinemaBD.Infrastructure`

### Vai trò

`CinemaBD.Infrastructure` là tầng hạ tầng kỹ thuật, chịu trách nhiệm cài đặt các interface từ Application. Đây là nơi xử lý database, JWT, thanh toán, seed dữ liệu và các service nghiệp vụ cụ thể.

### Thành phần chính

```text
CinemaBD.Infrastructure/
├── DependencyInjection/
├── Persistence/
├── Payments/
├── Security/
└── Services/
```

### Các thành phần tiêu biểu

#### Persistence

```text
AppDbContext.cs
DatabaseInitializer.cs
```

- `AppDbContext`: DbContext chính kết nối SQL Server.
- `DatabaseInitializer`: tự khởi tạo database từ `CinemaBD.sql` nếu database chưa có bảng.

#### Security

```text
JwtTokenService.cs
Md5PasswordHasher.cs
```

- Sinh JWT token.
- Hash/kiểm tra mật khẩu.

#### Payments

```text
VnPayUrlBuilder.cs
VnPaySignatureValidator.cs
```

- Tạo URL thanh toán VNPAY.
- Xác thực chữ ký callback thanh toán.

#### Services

```text
AuthService.cs
MovieService.cs
ShowtimeService.cs
SeatService.cs
BookingService.cs
PaymentService.cs
ReviewService.cs
AdminMovieService.cs
AdminDashboardService.cs
AdminInvoiceService.cs
AdminStatisticsService.cs
```

### Đăng ký Dependency Injection

File đăng ký DI:

```text
CinemaBD.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
```

Ví dụ:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

services.AddScoped<IMovieService, MovieService>();
services.AddScoped<IShowtimeService, ShowtimeService>();
services.AddScoped<ISeatService, SeatService>();
services.AddScoped<IBookingService, BookingService>();
services.AddScoped<IPaymentService, PaymentService>();
services.AddScoped<IReviewService, ReviewService>();
```

### Trách nhiệm

- Làm việc với SQL Server thông qua EF Core.
- Cài đặt các nghiệp vụ đặt vé, phim, suất chiếu, thanh toán.
- Tạo token JWT.
- Xử lý thanh toán VNPAY.
- Seed dữ liệu ban đầu.
- Chạy background service để xử lý suất chiếu quá hạn.

---

## 3.4. Tầng API - `CinemaBD.Api`

### Vai trò

`CinemaBD.Api` là tầng cung cấp REST API cho hệ thống. Tầng này nhận HTTP request, validate dữ liệu đầu vào cơ bản, gọi service nghiệp vụ và trả về response.

### Thành phần chính

```text
CinemaBD.Api/
├── Controllers/
├── Controllers/Admin/
├── Contracts/
├── Extensions/
├── Middleware/
└── Program.cs
```

### Controller khách hàng

```text
AuthController.cs
AccountController.cs
MoviesController.cs
BookingsController.cs
CombosController.cs
PaymentsController.cs
ReviewsController.cs
```

### Controller admin

```text
AdminAuthController.cs
AdminDashboardController.cs
AdminMoviesController.cs
AdminShowtimesController.cs
AdminRoomsController.cs
AdminSeatsController.cs
AdminCombosController.cs
AdminInvoicesController.cs
AdminStatisticsController.cs
AdminVouchersController.cs
AdminLoyaltyPointsController.cs
AdminRefundsController.cs
```

### Contracts

Thư mục `Contracts` chứa request/response DTO cho API:

```text
Contracts/Auth
Contracts/Booking
Contracts/Movies
Contracts/Payments
Contracts/Reviews
Contracts/Admin
Contracts/Common
```

Ví dụ:

```text
LoginRequest.cs
RegisterRequest.cs
CheckoutRequest.cs
CheckoutResponse.cs
MovieResponse.cs
PaymentCallbackResponse.cs
ApiResponse.cs
```

### Trách nhiệm

- Cung cấp endpoint REST.
- Cấu hình Swagger.
- Cấu hình JWT Bearer Authentication.
- Gọi service thông qua interface.
- Trả về dữ liệu dạng JSON.
- Xử lý exception qua middleware.

### Cấu hình trong `Program.cs`

```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);
```

API chạy Docker tại:

```text
http://localhost:5188/swagger
```

---

## 3.5. Tầng Web - `CinemaBD.Web`

### Vai trò

`CinemaBD.Web` là tầng giao diện người dùng, được xây dựng bằng ASP.NET Core MVC/Razor. Tầng này phục vụ cả giao diện khách hàng và giao diện quản trị.

### Thành phần chính

```text
CinemaBD.Web/
├── Controllers/
├── Areas/Admin/Controllers/
├── Views/
├── Areas/Admin/Views/
├── Core/
├── Services/
├── Hubs/
├── Models/
├── Data/
├── wwwroot/
└── Program.cs
```

### Controller khách hàng

```text
HomeController.cs
AccountController.cs
BookingController.cs
```

### Controller admin

```text
DashboardController.cs
MoviesController.cs
ShowtimesController.cs
BookingsController.cs
UsersController.cs
CombosController.cs
InvoiceControllers.cs
StatisticsControllers.cs
VoucherController.cs
LoyaltyPointsController.cs
RefundController.cs
ReviewsController.cs
```

### Core services Web

```text
AuthCoreService.cs
BookingCoreService.cs
AdminCoreServices.cs
AdminInterfaces.cs
CinemaApiClient.cs
```

Các service này giúp controller Web không thao tác trực tiếp quá nhiều với database/API.

### SignalR Hub

```text
SeatHub.cs
```

Dùng để cập nhật trạng thái ghế realtime trong quá trình đặt vé.

### Trách nhiệm

- Render giao diện Razor.
- Xử lý luồng điều hướng người dùng.
- Gọi API qua `CinemaApiClient`.
- Quản lý session đăng nhập web/admin.
- Hiển thị sơ đồ ghế, combo, thanh toán, dashboard admin.
- Kết nối SignalR realtime.

Web chạy Docker tại:

```text
http://localhost:7188
```

---

## 4. Luồng xử lý nghiệp vụ chính

## 4.1. Luồng đăng nhập khách hàng

```text
Browser
  -> CinemaBD.Web AccountController
  -> CinemaApiClient/AuthCoreService
  -> CinemaBD.Api AuthController
  -> IAuthService
  -> AuthService
  -> AppDbContext SQL Server
  -> JwtTokenService
  -> Trả token/thông tin user
```

Ý nghĩa:

- Web nhận thông tin đăng nhập.
- API xác thực tài khoản.
- Infrastructure kiểm tra database và sinh JWT.
- Web lưu thông tin cần thiết vào session/cookie.

---

## 4.2. Luồng xem danh sách phim

```text
Browser
  -> CinemaBD.Web HomeController
  -> CinemaApiClient hoặc service web
  -> CinemaBD.Api MoviesController
  -> IMovieService
  -> MovieService
  -> AppDbContext
  -> SQL Server
```

Dữ liệu phim được trả về và render trên giao diện danh sách phim/trang chủ.

---

## 4.3. Luồng đặt vé

```text
Khách hàng
  -> Chọn phim
  -> Chọn suất chiếu
  -> Chọn ghế
  -> Chọn combo
  -> Xác nhận checkout
  -> BookingController
  -> CinemaBD.Api BookingsController
  -> IBookingService
  -> BookingService
  -> AppDbContext
  -> SQL Server
  -> Tạo hóa đơn / cập nhật ghế
```

Các thành phần tham gia:

| Thành phần | Vai trò |
|---|---|
| Web BookingController | Điều hướng giao diện đặt vé |
| SeatHub | Cập nhật trạng thái ghế realtime |
| BookingsController | Nhận request checkout |
| IBookingService | Interface nghiệp vụ đặt vé |
| BookingService | Xử lý đặt vé thực tế |
| AppDbContext | Lưu hóa đơn, ghế, combo |

---

## 4.4. Luồng thanh toán VNPAY

```text
Khách hàng xác nhận thanh toán
  -> Web BookingController
  -> API PaymentsController
  -> IPaymentService
  -> PaymentService
  -> VnPayUrlBuilder
  -> Chuyển sang cổng VNPAY
  -> VNPAY callback return
  -> VnPaySignatureValidator
  -> Cập nhật hóa đơn
```

Mục tiêu:

- Tạo URL thanh toán đúng định dạng.
- Xác thực callback từ cổng thanh toán.
- Cập nhật trạng thái hóa đơn khi thanh toán thành công.

---

## 4.5. Luồng quản trị phim

```text
Admin
  -> CinemaBD.Web Areas/Admin/MoviesController
  -> CinemaBD.Api AdminMoviesController
  -> IAdminMovieService
  -> AdminMovieService
  -> AppDbContext
  -> SQL Server
```

Chức năng:

- Xem danh sách phim.
- Thêm phim.
- Sửa phim.
- Xóa hoặc đổi trạng thái phim.

---

## 5. Sơ đồ kiến trúc triển khai Docker

Khi chạy bằng Docker Compose, hệ thống gồm 3 container chính:

```text
┌──────────────────────┐
│ Host Machine          │
│                      │
│  http://localhost:7188
│          │           │
│          v           │
│  ┌───────────────┐   │
│  │ cinemabd_web  │   │
│  │ port 8090     │   │
│  └───────┬───────┘   │
│          │ http://api:8080
│          v           │
│  ┌───────────────┐   │
│  │ cinemabd_api  │   │
│  │ port 8080     │   │
│  └───────┬───────┘   │
│          │ sqlserver:1433
│          v           │
│  ┌───────────────────┐│
│  │ cinemabd_sqlserver││
│  │ SQL Server 2022   ││
│  └───────────────────┘│
└──────────────────────┘
```

Port public:

| Container | Port trong container | Port ngoài máy |
|---|---:|---:|
| Web | 8090 | 7188 |
| API | 8080 | 5188 |
| SQL Server | 1433 | 1433 |

---

## 6. Mapping tầng với thư mục source code

| Tầng | Project/Thư mục | Công nghệ | Vai trò |
|---|---|---|---|
| Presentation Web | `CinemaBD.Web` | ASP.NET Core MVC/Razor | Giao diện khách hàng/admin |
| Presentation API | `CinemaBD.Api` | ASP.NET Core Web API | REST endpoint, Swagger, JWT |
| Application | `CinemaBD.Application` | C# Interface | Khai báo nghiệp vụ |
| Domain | `CinemaBD.Domain` | C# Entity/Enum | Model nghiệp vụ lõi |
| Infrastructure | `CinemaBD.Infrastructure` | EF Core, SQL Server, JWT, Payment | Cài đặt nghiệp vụ và hạ tầng |
| Database | SQL Server/SQLite | SQL | Lưu trữ dữ liệu |
| Deployment | Docker Compose | Docker | Triển khai local/demo |

---

## 7. Ưu điểm của kiến trúc phân tầng trong CinemaBD

- **Dễ bảo trì:** mỗi tầng có trách nhiệm rõ ràng.
- **Dễ mở rộng:** có thể thêm module voucher, tích điểm, refund mà không phá cấu trúc chung.
- **Dễ kiểm thử:** service có interface nên dễ mock khi test.
- **Giảm phụ thuộc:** controller không phụ thuộc trực tiếp vào DbContext của API.
- **Phù hợp triển khai thực tế:** tách Web, API, Database rõ ràng.
- **Dễ tích hợp:** API có thể dùng cho mobile app hoặc frontend SPA sau này.

---

## 8. Hạn chế hiện tại và hướng cải tiến kiến trúc

## 8.1. Hạn chế

- `CinemaBD.Web` vẫn còn một phần sử dụng SQLite local.
- Một số nghiệp vụ web/admin chưa chuyển hoàn toàn sang gọi API.
- Chưa có project test riêng cho unit/integration test.
- Chưa có CI/CD tự động.

## 8.2. Hướng cải tiến

- Chuyển toàn bộ Web sang gọi `CinemaBD.Api` qua `CinemaApiClient`.
- Tách DTO dùng chung sang project riêng nếu API/Web cần chia sẻ contract.
- Thêm `CinemaBD.Tests` để kiểm thử service, booking, payment.
- Thêm Redis cache/session nếu chạy production nhiều instance.
- Thêm background job cho giữ ghế, hủy ghế quá hạn, gửi email bất đồng bộ.
- Triển khai reverse proxy Nginx/Caddy và HTTPS khi deploy thật.

---

## 9. Kết luận

Kiến trúc phân tầng của CinemaBD giúp dự án có cấu trúc rõ ràng, phù hợp với đồ án website đặt vé xem phim. Các tầng `Domain`, `Application`, `Infrastructure`, `Api` và `Web` được tách theo trách nhiệm riêng, giúp code dễ đọc, dễ bảo trì và dễ mở rộng. Với cách tổ chức này, hệ thống có thể tiếp tục phát triển thêm các chức năng như voucher, tích điểm, realtime dashboard, kiểm vé QR và triển khai production bằng Docker.
