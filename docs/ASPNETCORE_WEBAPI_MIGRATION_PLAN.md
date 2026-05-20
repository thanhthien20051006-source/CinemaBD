# Kế hoạch migrate sang ASP.NET Core Web API

## Đã tạo
- `CinemaBD.Api`
- `CinemaBD.Application`
- `CinemaBD.Domain`
- `CinemaBD.Infrastructure`

## Mục tiêu phase kế tiếp
### Phase 1 - migrate contract + auth
- Hoàn thiện JWT auth
- Tạo DTO response chuẩn
- Tạo user/admin auth thật

### Phase 2 - migrate movie/showtime
- map entity phim
- map entity suất chiếu
- API lấy danh sách phim và suất chiếu

### Phase 3 - migrate booking
- lấy ghế đã đặt
- tạo checkout
- sinh payment URL
- callback payment
- hóa đơn

### Phase 4 - migrate admin
- CRUD phim
- CRUD suất chiếu
- CRUD khách hàng
- CRUD nhân viên
- thống kê

## Nguyên tắc
- Không phá project MVC 5 cũ
- Migrate từng module nhỏ
- Ưu tiên backend chạy được bằng Swagger trước khi làm frontend
