# CinemaBD - Kiến trúc phân tầng

## 1. Trạng thái sau refactor
Dự án đã được tách theo kiến trúc phân tầng trên .NET 8:

```text
CinemaBD.WebApi.sln
├── CinemaBD.Domain          # Entity, enum, model lõi
├── CinemaBD.Application     # Interface/use-case contract
├── CinemaBD.Infrastructure  # EF Core, JWT, payment, service implementation
├── CinemaBD.Api             # REST API controller
└── CinemaBD.Web             # MVC Web UI
```

Ngoài ra còn thư mục `CinemaBD/` là source MVC 5 legacy để tham khảo/migration, không phải luồng chính của solution .NET 8.

## 2. Quy tắc dependency

```text
Api ───────────────┐
                   ▼
Web          Infrastructure ───► Application ───► Domain
                                   ▲
                                   └──────────── contracts only
```

- `Domain`: không phụ thuộc tầng nào.
- `Application`: chỉ phụ thuộc `Domain`, chứa interface như `IMovieService`, `IBookingService`, `IAuthService`.
- `Infrastructure`: phụ thuộc `Application` + `Domain`, chứa `AppDbContext`, JWT, VNPAY, implementation service.
- `Api`: gọi service qua interface, cấu hình DI bằng `AddInfrastructure()`.
- `Web`: UI MVC riêng, hiện vẫn có một phần code demo/local SQLite.

## 3. Thay đổi đã làm

### 3.1 Di chuyển implementation service
Các file service implementation đã được chuyển từ:

```text
CinemaBD.Application/Services/*.cs
```

sang đúng tầng hạ tầng:

```text
CinemaBD.Infrastructure/Services/*.cs
```

Lý do: các service này đang dùng `AppDbContext`, EF Core, hashing, payment... nên không nên nằm trong `Application`.

### 3.2 Làm sạch project file
Đã bỏ cấu hình link tạm:

```xml
<Compile Include="..\CinemaBD.Application\Services\*.cs" Link="MigratedServices\%(Filename)%(Extension)" />
```

và bỏ rule exclude service trong `CinemaBD.Application.csproj`.

### 3.3 Cập nhật DI
`CinemaBD.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` giờ dùng namespace:

```csharp
using CinemaBD.Infrastructure.Services;
```

và đăng ký service implementation tại Infrastructure.

## 4. Vai trò từng tầng

### Domain
Chứa dữ liệu nghiệp vụ lõi:
- `Entities/Movie.cs`
- `Entities/Booking.cs`
- `Entities/Showtime.cs`
- `Entities/Seat.cs`
- `Enums/BookingStatus.cs`

Không đặt EF Core, controller, HTTP, JWT, VNPAY ở đây.

### Application
Chứa hợp đồng nghiệp vụ:
- `Interfaces/IAuthService.cs`
- `Interfaces/IBookingService.cs`
- `Interfaces/IMovieService.cs`
- `Interfaces/IPaymentService.cs`

Không truy cập DB trực tiếp ở tầng này trong trạng thái hiện tại.

### Infrastructure
Chứa code kỹ thuật + implementation:
- `Persistence/AppDbContext.cs`
- `Security/JwtTokenService.cs`
- `Security/Md5PasswordHasher.cs`
- `Payments/VnPayUrlBuilder.cs`
- `Services/*Service.cs`

### Api
Chứa endpoint REST:
- `Controllers/AuthController.cs`
- `Controllers/MoviesController.cs`
- `Controllers/BookingsController.cs`
- `Controllers/PaymentsController.cs`

Controller chỉ nhận request, gọi interface service, trả response.

### Web
Chứa giao diện MVC Razor. Nếu tiếp tục chuẩn hóa, nên cho Web gọi `CinemaBD.Api` qua `CinemaApiClient` hoặc tách dần code local sang Application/Infrastructure.

## 5. Cập nhật Web layer

Đã tách `CinemaBD.Web/Core/Services.cs` thành các file nhỏ hơn:

```text
CinemaBD.Web/Core/AuthCoreService.cs
CinemaBD.Web/Core/BookingCoreService.cs
CinemaBD.Web/Core/AdminInterfaces.cs
CinemaBD.Web/Core/AdminCoreServices.cs
```

Các controller admin/customer trong `CinemaBD.Web` không còn inject `CinemaDbContext` trực tiếp. Controller chỉ gọi service/interface:

- `IAdminAuthCoreService`
- `IAdminDashboardCoreService`
- `IAdminBookingCoreService`
- `IAdminMovieCoreService`
- `IAdminShowtimeCoreService`
- `IAdminComboCoreService`
- `IAdminUserCoreService`
- `CinemaApiClient`

`BookingController.PaymentReturn` đã đổi sang gọi API payment qua `CinemaApiClient.ConfirmPaymentAsync(...)`, không gọi thẳng DB/service fallback trong controller.

Lưu ý: thư mục MVC 5 legacy `CinemaBD/` không bị sửa. Chỉ làm trong solution .NET 8 `CinemaBD.Web`, đúng phạm vi giao diện khách hàng/admin.

## 6. Kết quả kiểm tra

Lệnh đã chạy:

```powershell
dotnet build D:\ALL_CNTT\CinemaBD\CinemaBD.WebApi.sln --no-restore
```

Kết quả:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 7. Chức năng đã phát triển thêm: Reviews

Đã thêm module đánh giá phim theo đúng phân tầng:

```text
Domain
└── Review.cs

Application
├── IReviewService.cs
└── IAdminReviewService.cs

Infrastructure
├── ReviewService.cs
└── AdminReviewService.cs

Api
├── Controllers/ReviewsController.cs
└── Contracts/Reviews/*

Web
├── HomeController.CreateReview
├── Views/Home/Details.cshtml
├── Areas/Admin/Controllers/ReviewsController.cs
└── Areas/Admin/Views/Reviews/Index.cshtml
```

Chức năng:

- Khách hàng xem danh sách đánh giá ở trang chi tiết phim.
- Khách hàng đăng nhập có thể gửi đánh giá phim.
- Admin có trang quản lý đánh giá: `/Admin/Reviews`.
- Admin có thể xóa đánh giá.
- Controller không dùng trực tiếp `DbContext`; truy cập dữ liệu nằm ở service.

## 8. Cập nhật đăng nhập chung khách hàng/admin

Đã gộp giao diện đăng nhập:

```text
/account/login
```

Luồng xử lý:

1. Người dùng nhập tài khoản/mật khẩu trên cùng một form.
2. Hệ thống thử đăng nhập admin qua `api/admin/auth/login`.
3. Nếu là admin: lưu session `AdminToken`, `AdminUser`, `AdminFullName`, `AdminRole`, chuyển đến `/Admin/Dashboard`.
4. Nếu không phải admin: thử đăng nhập khách hàng qua `api/auth/login`.
5. Nếu là khách hàng: lưu session `UserToken`, `Username`, `FullName`, `UserId`, chuyển về trang khách hàng.

Admin login cũ `/Admin/AdminAccount/Login` được redirect về form chung `/account/login`.

## 9. Việc nên làm tiếp

1. Nếu muốn Web thuần frontend: chuyển nốt admin sang gọi REST API thay vì local SQLite service.
2. Bổ sung API admin còn thiếu nếu cần quản trị hoàn toàn qua `CinemaBD.Api`.
3. Không đưa business logic mới vào Controller.
4. Không đưa EF Core/DbContext vào `Application`.
5. Chỉ sửa thư mục MVC 5 legacy `CinemaBD/` ở phần giao diện khách hàng/admin khi được yêu cầu rõ.
