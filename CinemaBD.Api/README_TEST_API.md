# README - Test CinemaBD Web API

## 1. Yêu cầu
- .NET 8 SDK
- SQL Server đang chạy
- Database hiện tại của CinemaBD đã có dữ liệu

## 2. Mở solution
Mở file:
- `CinemaBD.WebApi.sln`

Hoặc chạy CLI tại thư mục gốc:
```bash
dotnet build CinemaBD.WebApi.sln
```

## 3. Cấu hình database
Mở file:
- `CinemaBD.Api/appsettings.json`

Sửa connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=CinemaBD;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Lưu ý: database nên trỏ về chính DB hiện tại của project MVC 5 nếu bạn muốn API đọc dữ liệu thật.

## 4. Cấu hình JWT
Trong `appsettings.json`, sửa:
```json
"Jwt": {
  "Issuer": "CinemaBD",
  "Audience": "CinemaBD.Client",
  "Key": "YOUR_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS"
}
```

## 5. Cấu hình VNPAY
Sửa phần:
```json
"VnPay": {
  "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
  "ReturnUrl": "https://localhost:7067/api/payments/vnpay-return",
  "TmnCode": "...",
  "HashSecret": "..."
}
```

## 6. Chạy API
```bash
dotnet run --project CinemaBD.Api/CinemaBD.Api.csproj
```

Swagger mặc định:
- `https://localhost:7067/swagger`
- `http://localhost:5067/swagger`

## 7. Test luồng User bằng Swagger/Postman
### Bước 1 - Register
`POST /api/auth/register`

Body mẫu:
```json
{
  "fullName": "Nguyen Van A",
  "username": "userapi01",
  "password": "123456",
  "email": "userapi01@example.com",
  "phoneNumber": "0900000001"
}
```

### Bước 2 - Login
`POST /api/auth/login`

Body mẫu:
```json
{
  "username": "userapi01",
  "password": "123456"
}
```

Copy token trả về.

### Bước 3 - Lấy danh sách phim
`GET /api/movies`

### Bước 4 - Lấy lịch chiếu của phim
`GET /api/movies/{id}/showtimes?date=2026-04-04`

### Bước 5 - Lấy sơ đồ ghế
`GET /api/bookings/showtimes/{showtimeId}/seats`

### Bước 6 - Authorize bằng JWT trong Swagger
- bấm **Authorize**
- nhập:
```text
Bearer <your_token>
```

### Bước 7 - Checkout
`POST /api/bookings/checkout`

Body mẫu:
```json
{
  "showtimeId": "SC20260404120000",
  "seats": ["A1", "A2"],
  "combos": "CB01:1,CB02:2",
  "totalAmount": 180000
}
```

API sẽ trả:
- `transactionRef`
- `paymentUrl`

### Bước 8 - Test callback VNPAY
Dùng URL callback sau khi thanh toán sandbox thành công hoặc mô phỏng query.

### Bước 9 - Lấy invoice
`GET /api/bookings/invoice/{txnRef}`

## 8. Test luồng Admin
### Bước 1 - Login admin
`POST /api/admin/auth/login`

Body mẫu:
```json
{
  "username": "admin",
  "password": "123456"
}
```

Copy token admin.

### Bước 2 - Authorize
Trong Swagger nhập:
```text
Bearer <admin_token>
```

### Bước 3 - Dashboard
`GET /api/admin/dashboard`

### Bước 4 - Movies CRUD
- `GET /api/admin/movies`
- `POST /api/admin/movies`
- `PUT /api/admin/movies/{id}`
- `DELETE /api/admin/movies/{id}`

### Bước 5 - Showtimes CRUD
- `GET /api/admin/showtimes?date=2026-04-04`
- `POST /api/admin/showtimes`
- `PUT /api/admin/showtimes/{id}`
- `DELETE /api/admin/showtimes/{id}`

## 9. Middleware lỗi toàn cục
API đã có `ExceptionHandlingMiddleware`.
Nếu xảy ra lỗi business hoặc lỗi hệ thống, response JSON trả dạng:
```json
{
  "success": false,
  "message": "...",
  "data": null
}
```

## 10. Nếu build lỗi
Chạy:
```bash
dotnet restore
 dotnet build CinemaBD.WebApi.sln
```

Nếu vẫn lỗi, copy toàn bộ lỗi build và gửi lại để sửa tiếp.
