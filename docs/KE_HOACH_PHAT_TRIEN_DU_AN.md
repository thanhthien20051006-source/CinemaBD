# KẾ HOẠCH PHÁT TRIỂN DỰ ÁN CINEMABD

## Đề tài

**Xây dựng hệ thống quản lý rạp phim Bình Dương Cinema**

Tài liệu này được xây dựng dựa trên các nguồn nghiên cứu đã tổng hợp: IEEE Xplore, ACM Digital Library, arXiv, Google Scholar, Martin Fowler, Refactoring.Guru, Awesome Software Architecture và InfoQ. Kế hoạch tập trung vào việc hoàn thiện hệ thống theo hướng thực tế, dễ bảo trì, dễ triển khai và phù hợp báo cáo đồ án.

---

## 1. Cơ sở xây dựng kế hoạch

Từ các tài liệu nghiên cứu, dự án nên phát triển theo các định hướng chính:

| Nguồn tham khảo | Bài học áp dụng |
|---|---|
| IEEE Xplore, ACM, Google Scholar | Hệ thống quản lý rạp phim cần có đặt vé, quản lý lịch chiếu, thanh toán, báo cáo và bảo mật |
| Martin Fowler | Kiến trúc tốt phải hỗ trợ mở rộng và bảo trì lâu dài |
| Refactoring.Guru | Áp dụng design pattern, tách trách nhiệm, giảm code phụ thuộc chặt |
| Awesome Software Architecture | Cần chú ý service boundary, testing, logging, monitoring |
| InfoQ | Nên chuẩn hóa Docker, DevOps, CI/CD và observability |

Dự án CinemaBD hiện đã có nền tảng:

```text
CinemaBD.Domain
CinemaBD.Application
CinemaBD.Infrastructure
CinemaBD.Api
CinemaBD.Web
Docker Compose
SQL Server
ASP.NET Core MVC/Web API
SignalR
VNPAY/Momo demo
```

Vì vậy kế hoạch phát triển nên đi theo hướng:

```text
Hoàn thiện nghiệp vụ -> Chuẩn hóa kiến trúc -> Tăng bảo mật -> Tối ưu UX -> Kiểm thử -> Triển khai production
```

---

# 2. Mục tiêu phát triển

## 2.1. Mục tiêu ngắn hạn

- Hoàn thiện các chức năng chính để demo đồ án.
- Đảm bảo website chạy ổn bằng Docker.
- Hoàn thiện báo cáo, tài liệu setup và tài liệu kiến trúc.
- Giảm lỗi trong luồng đặt vé/thanh toán.
- Chuẩn hóa dữ liệu mẫu để hội đồng có thể test nhanh.

## 2.2. Mục tiêu trung hạn

- Chuẩn hóa Web gọi API thay vì xử lý phân tán.
- Hoàn thiện realtime giữ ghế.
- Hoàn thiện vé điện tử QR/PDF/email.
- Bổ sung voucher, tích điểm, phân quyền.
- Có unit test/integration test cho nghiệp vụ quan trọng.

## 2.3. Mục tiêu dài hạn

- Triển khai production có domain, HTTPS, reverse proxy.
- Có CI/CD tự động build/test/deploy.
- Có logging, monitoring, health check.
- Có backup database định kỳ.
- Có thể mở rộng sang mobile app hoặc frontend SPA.

---

# 3. Lộ trình phát triển theo giai đoạn

## Giai đoạn 1: Ổn định nền tảng và dữ liệu demo

### Mục tiêu

Đảm bảo dự án có thể tải về, chạy được ngay, có dữ liệu mẫu và không lỗi build.

### Công việc

| STT | Công việc | Kết quả cần đạt |
|---:|---|---|
| 1 | Kiểm tra lại Docker Compose | Chạy được `web`, `api`, `sqlserver` |
| 2 | Chuẩn hóa `.env.example` | Không chứa secret thật |
| 3 | Chuẩn hóa seed database | SQL Server tự import `CinemaBD.sql` khi DB trống |
| 4 | Kiểm tra tài khoản demo | Có tài khoản admin/user để test |
| 5 | Build solution | `0 error` |
| 6 | Cập nhật README | Có hướng dẫn setup Docker rõ ràng |

### Ưu tiên

Cao.

### Tiêu chí hoàn thành

```bash
dotnet build CinemaBD.WebApi.sln --no-restore
docker compose config --quiet
docker compose up -d --build
```

Website mở được:

```text
http://localhost:7188
http://localhost:5188/swagger
```

---

## Giai đoạn 2: Hoàn thiện nghiệp vụ đặt vé

### Mục tiêu

Luồng đặt vé là nghiệp vụ lõi của hệ thống rạp phim, cần được ưu tiên hoàn thiện.

### Công việc

| STT | Công việc | Mô tả |
|---:|---|---|
| 1 | Đồng bộ sơ đồ ghế khách/admin | Cùng layout, cùng trạng thái ghế |
| 2 | Giữ ghế tạm thời | Khi khách chọn ghế, ghế chuyển trạng thái `Đang giữ` |
| 3 | Tự hủy ghế giữ quá hạn | Sau 5-10 phút không thanh toán thì trả ghế |
| 4 | Chống đặt trùng ghế | Kiểm tra lại trạng thái ghế tại bước checkout |
| 5 | Cập nhật realtime bằng SignalR | Người khác thấy ghế vừa được chọn/đặt |
| 6 | Chuẩn hóa trạng thái vé | Pending, Paid, Cancelled, Refunded |

### Luồng chuẩn đề xuất

```text
Chọn phim
  -> Chọn suất chiếu
  -> Chọn ghế
  -> Giữ ghế tạm thời
  -> Chọn combo
  -> Thanh toán
  -> Xác nhận thanh toán
  -> Cập nhật ghế đã đặt
  -> Sinh vé điện tử
```

### Tiêu chí hoàn thành

- Không đặt trùng ghế khi mở 2 trình duyệt cùng lúc.
- Ghế đang giữ được hiển thị realtime.
- Nếu không thanh toán, ghế tự trả về trạng thái trống.

---

## Giai đoạn 3: Hoàn thiện thanh toán và vé điện tử

### Mục tiêu

Tăng tính thực tế của hệ thống bằng thanh toán, hóa đơn, QR và email.

### Công việc

| STT | Công việc | Mô tả |
|---:|---|---|
| 1 | Hoàn thiện VNPAY return | Xác nhận giao dịch sau thanh toán |
| 2 | Thêm VNPAY IPN | Nhận xác nhận server-to-server nếu deploy public |
| 3 | Chống callback lặp | Idempotency để không cộng tiền/tạo vé nhiều lần |
| 4 | Chuẩn hóa MoMo sandbox | Cấu hình qua `.env` |
| 5 | Sinh QR vé | QR chứa mã vé/check-in code |
| 6 | Xuất PDF vé/hóa đơn | File vé có thông tin phim, ghế, suất chiếu |
| 7 | Gửi email vé | Gửi QR/PDF cho khách sau thanh toán |
| 8 | Check-in vé | Admin/nhân viên nhập hoặc quét mã vé để kiểm tra |

### Tiêu chí hoàn thành

- Thanh toán thành công cập nhật đúng hóa đơn.
- Refresh callback không tạo hóa đơn/vé trùng.
- Vé có QR dùng được cho kiểm vé.
- Email gửi được khi cấu hình SMTP hợp lệ.

---

## Giai đoạn 4: Hoàn thiện quản trị rạp phim

### Mục tiêu

Admin có đủ công cụ để vận hành rạp phim Bình Dương Cinema.

### Công việc

| Nhóm chức năng | Công việc |
|---|---|
| Quản lý phim | CRUD phim, thể loại, trạng thái phim |
| Quản lý suất chiếu | Tạo lịch chiếu, kiểm tra trùng phòng/giờ |
| Quản lý phòng ghế | Tạo phòng, sơ đồ ghế, ghế VIP, ghế bảo trì |
| Quản lý khách hàng | Xem lịch sử đặt vé, trạng thái tài khoản |
| Quản lý nhân viên | Tài khoản nhân viên, vai trò, quyền |
| Quản lý hóa đơn | Xem, lọc, chi tiết, hoàn tiền |
| Quản lý combo | CRUD combo bắp nước |
| Quản lý đánh giá | Duyệt/ẩn đánh giá không phù hợp |
| Quản lý voucher | Tạo mã giảm giá, giới hạn lượt dùng |
| Quản lý tích điểm | Cộng/trừ điểm, hạng thành viên |

### Tiêu chí hoàn thành

- Admin thao tác được toàn bộ nghiệp vụ chính.
- Controller không chứa business logic phức tạp.
- Service xử lý nghiệp vụ, API/Web chỉ điều hướng.

---

## Giai đoạn 5: Báo cáo, thống kê và dashboard

### Mục tiêu

Tăng giá trị quản lý cho hệ thống.

### Công việc

| STT | Báo cáo | Nội dung |
|---:|---|---|
| 1 | Doanh thu ngày/tháng/năm | Tổng doanh thu theo thời gian |
| 2 | Vé bán theo phim | Top phim bán chạy |
| 3 | Tỷ lệ lấp đầy phòng | Số ghế bán / tổng ghế |
| 4 | Doanh thu combo | Combo bán chạy |
| 5 | Hiệu quả voucher | Số lượt dùng, doanh thu giảm giá |
| 6 | Khách hàng thân thiết | Top khách hàng theo vé/điểm |
| 7 | Xuất Excel/PDF | Tải báo cáo cho admin |

### Dashboard đề xuất

```text
- Doanh thu hôm nay
- Số vé bán hôm nay
- Suất chiếu đang diễn ra
- Top phim trong tuần
- Tỷ lệ ghế đã đặt
- Giao dịch gần nhất
```

### Tiêu chí hoàn thành

- Admin xem được số liệu trực quan.
- Có lọc theo ngày/tháng/năm.
- Có xuất báo cáo Excel hoặc PDF.

---

## Giai đoạn 6: Chuẩn hóa kiến trúc và chất lượng code

### Mục tiêu

Theo định hướng từ Martin Fowler và Refactoring.Guru: kiến trúc phải dễ bảo trì, dễ mở rộng, ít phụ thuộc chặt.

### Công việc

| STT | Công việc | Mục tiêu |
|---:|---|---|
| 1 | Chuyển Web gọi API hoàn toàn | Giảm phụ thuộc SQLite local |
| 2 | Tách DTO rõ ràng | Request/Response không dùng trực tiếp entity |
| 3 | Không đặt business logic trong Controller | Controller chỉ nhận request/trả response |
| 4 | Chuẩn hóa service interface | Application chỉ chứa contract |
| 5 | Tách module theo nghiệp vụ | Movie, Booking, Payment, Admin, Report |
| 6 | Thêm validation | Validate input trước khi xử lý |
| 7 | Thêm exception middleware | Response lỗi thống nhất |
| 8 | Viết test | Unit test và integration test |

### Quy tắc code

```text
Domain: Entity/Enum, không phụ thuộc framework
Application: Interface/Contract
Infrastructure: EF Core, service implementation, payment, JWT
Api: Controller, DTO, middleware
Web: MVC/Razor, gọi API, hiển thị giao diện
```

### Tiêu chí hoàn thành

- Build không lỗi.
- Không có DbContext trong Application.
- Controller không xử lý nghiệp vụ dài.
- Các module quan trọng có test.

---

## Giai đoạn 7: Bảo mật và phân quyền

### Mục tiêu

Bảo vệ dữ liệu người dùng, giao dịch và khu vực quản trị.

### Công việc

| STT | Công việc | Mô tả |
|---:|---|---|
| 1 | Bảo mật secret | Dùng `.env`, không commit secret thật |
| 2 | JWT chuẩn | Key đủ mạnh, issuer/audience rõ ràng |
| 3 | Role-based authorization | Admin/Employee/Customer |
| 4 | Phân quyền chi tiết | Quyền theo module: phim, vé, hóa đơn, thống kê |
| 5 | Validate input | Tránh dữ liệu sai hoặc tấn công cơ bản |
| 6 | Log giao dịch | Lưu lịch sử thanh toán, refund, check-in |
| 7 | HTTPS production | Không chạy HTTP khi deploy thật |
| 8 | Backup database | Sao lưu SQL Server định kỳ |

### Tiêu chí hoàn thành

- Không còn secret thật trong repo.
- Admin/user không truy cập sai quyền.
- Giao dịch quan trọng có log.

---

## Giai đoạn 8: UX/UI và trải nghiệm người dùng

### Mục tiêu

Website dễ dùng, rõ luồng đặt vé, phù hợp demo và sử dụng thực tế.

### Công việc

| Khu vực | Cải tiến |
|---|---|
| Trang chủ | Hiển thị phim đang chiếu/sắp chiếu rõ hơn |
| Chi tiết phim | Trailer, lịch chiếu, đánh giá |
| Chọn ghế | Legend rõ, màu ghế thống nhất, responsive mobile |
| Checkout | Hiển thị tổng tiền, combo, phí, giảm giá |
| Thanh toán thành công | Hiển thị vé, QR, nút tải PDF |
| Vé của tôi | Danh sách vé dạng card, lọc theo trạng thái |
| Admin | Dashboard gọn, bảng có search/filter/pagination |

### Tiêu chí hoàn thành

- Người dùng mới có thể đặt vé không cần hướng dẫn.
- Giao diện chạy tốt trên desktop và mobile.
- Admin dễ tìm dữ liệu.

---

## Giai đoạn 9: DevOps, kiểm thử và triển khai

### Mục tiêu

Theo định hướng từ InfoQ và Awesome Software Architecture: hệ thống cần dễ triển khai, dễ quan sát, dễ kiểm thử.

### Công việc

| STT | Công việc | Kết quả |
|---:|---|---|
| 1 | Docker production | Compose cho môi trường thật |
| 2 | Reverse proxy | Nginx/Caddy |
| 3 | HTTPS | Domain + SSL |
| 4 | CI/CD GitHub Actions | Tự động build/test khi push |
| 5 | Health check | API/database status |
| 6 | Logging | Log lỗi, log thanh toán |
| 7 | Monitoring | Theo dõi CPU/RAM/request/error |
| 8 | Backup | Backup SQL Server định kỳ |

### Pipeline đề xuất

```text
Push code lên GitHub
  -> Restore packages
  -> Build solution
  -> Run tests
  -> Build Docker images
  -> Deploy server
```

---

# 4. Kế hoạch ưu tiên thực hiện

## 4.1. Ưu tiên cho đồ án/demo

Nên làm theo thứ tự:

```text
1. Docker chạy ổn
2. Dữ liệu mẫu đầy đủ
3. Đặt vé không lỗi
4. Thanh toán demo ổn
5. Vé QR/PDF/email
6. Admin quản lý phim/suất chiếu/ghế/hóa đơn
7. Thống kê cơ bản
8. Báo cáo tài liệu hoàn chỉnh
```

## 4.2. Ưu tiên kỹ thuật

```text
1. Không business logic trong Controller
2. Không DbContext trong Application
3. Service theo interface
4. DTO rõ ràng
5. Secret qua .env
6. Test luồng booking/payment
7. Docker + README rõ ràng
```

---

# 5. Bảng timeline đề xuất

## Nếu còn 2 tuần

| Thời gian | Việc cần làm |
|---|---|
| Ngày 1-2 | Fix Docker, seed data, tài khoản demo |
| Ngày 3-5 | Hoàn thiện đặt vé, chọn ghế, chống đặt trùng |
| Ngày 6-7 | Hoàn thiện thanh toán, QR/PDF/email |
| Ngày 8-9 | Hoàn thiện admin phim/suất chiếu/hóa đơn/thống kê |
| Ngày 10 | Test toàn bộ flow |
| Ngày 11-12 | Hoàn thiện báo cáo, hình ảnh, sơ đồ |
| Ngày 13 | Chuẩn bị slide demo |
| Ngày 14 | Tổng duyệt demo |

## Nếu còn 1 tháng

| Tuần | Mục tiêu |
|---|---|
| Tuần 1 | Ổn định Docker, database, kiến trúc, tài liệu setup |
| Tuần 2 | Hoàn thiện booking, seat realtime, payment |
| Tuần 3 | Hoàn thiện admin, voucher, tích điểm, report |
| Tuần 4 | Test, UI polish, báo cáo, slide, demo |

---

# 6. Rủi ro và cách xử lý

| Rủi ro | Ảnh hưởng | Cách xử lý |
|---|---|---|
| Docker SQL Server khởi động chậm | Web/API lỗi lúc đầu | Dùng healthcheck và retry |
| Thanh toán cần public callback | VNPAY IPN không test local được | Dùng ngrok hoặc chỉ demo return URL |
| Đặt trùng ghế | Sai nghiệp vụ nghiêm trọng | Lock/transaction/check trạng thái trước checkout |
| Secret bị commit | GitHub chặn push, mất an toàn | Dùng `.env`, rotate key nếu lộ |
| Dữ liệu demo thiếu | Demo khó thuyết phục | Chuẩn hóa seed SQL |
| Controller quá nhiều logic | Khó bảo trì | Đẩy logic xuống service |
| UI mobile chưa tốt | Trải nghiệm kém | Ưu tiên responsive các trang chính |

---

# 7. Kết quả cuối cùng cần đạt

Khi hoàn thành kế hoạch, dự án cần đạt:

- Website quản lý rạp phim chạy được bằng Docker.
- Khách hàng đặt vé được từ đầu đến cuối.
- Admin quản lý được dữ liệu rạp phim.
- Có thanh toán demo và vé điện tử.
- Có dashboard/thống kê cơ bản.
- Có kiến trúc phân tầng rõ ràng.
- Có tài liệu setup, báo cáo 4 chương, tài liệu kiến trúc và tài liệu tham khảo.
- Source code sạch, không chứa secret thật.
- Repo GitHub dùng được cho người khác tải về chạy.

---

# 8. Kết luận

Dựa trên các tài liệu nghiên cứu, hướng phát triển phù hợp nhất cho đề tài **Xây dựng hệ thống quản lý rạp phim Bình Dương Cinema** là tiếp tục hoàn thiện theo kiến trúc phân tầng hiện có, ưu tiên nghiệp vụ đặt vé, thanh toán, quản trị và triển khai Docker. Sau khi ổn định chức năng chính, dự án nên mở rộng sang kiểm thử, bảo mật, CI/CD, monitoring và triển khai production.

Kế hoạch này vừa phù hợp để hoàn thành đồ án, vừa tạo nền tảng để phát triển thành hệ thống thực tế trong tương lai.
