# CinemaBD.Api

Dự án **ASP.NET Core Web API** được migrate dần từ hệ thống MVC 5 hiện tại.

## Tệp quan trọng
- `README_TEST_API.md` - hướng dẫn build/run/test API chi tiết
- `appsettings.json` - cấu hình database, JWT, VNPAY, email
- `Program.cs` - cấu hình DI, JWT, middleware, Swagger
- `Middleware/ExceptionHandlingMiddleware.cs` - xử lý lỗi toàn cục

## Kiến trúc solution
- `CinemaBD.WebApi.sln`
- `CinemaBD.Api`
- `CinemaBD.Application`
- `CinemaBD.Domain`
- `CinemaBD.Infrastructure`

## Các module đã migrate
### Public
- Auth thật
- Movies thật
- Showtimes thật
- Seats thật
- Checkout thật
- Payment callback thật
- Invoice thật

### Admin
- Admin auth thật
- Admin dashboard thật
- Admin movies CRUD thật
- Admin showtimes CRUD thật
- Admin customers CRUD thật
- Admin employees CRUD thật
- Admin roles CRUD thật
- Admin permissions assign/remove thật

## Ghi chú
Đây là backend mới song song với project MVC 5 cũ. Có thể tiếp tục nâng cấp validation, logging, policy-based authorization, email invoice, refresh token và frontend tiêu thụ API.
