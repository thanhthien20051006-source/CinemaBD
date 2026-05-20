# NGHIÊN CỨU TÀI LIỆU THAM KHẢO CHO ĐỀ TÀI CINEMABD

## Đề tài

**Xây dựng hệ thống quản lý rạp phim Bình Dương Cinema**

Tài liệu này tổng hợp nội dung học được từ các nguồn tham khảo mà đề tài có thể sử dụng để viết phần cơ sở lý thuyết, kiến trúc hệ thống, công nghệ sử dụng và tài liệu tham khảo trong báo cáo đồ án.

---

## 1. Mục tiêu nghiên cứu tài liệu

Mục tiêu của việc tham khảo các nguồn học thuật và kỹ thuật là:

- Hiểu cách xây dựng hệ thống quản lý/đặt vé rạp phim.
- Chọn kiến trúc phần mềm phù hợp cho dự án CinemaBD.
- Bổ sung cơ sở lý thuyết về kiến trúc phân tầng, REST API, design pattern, Docker và triển khai hệ thống.
- Có nguồn tham khảo đáng tin cậy để đưa vào báo cáo đồ án.
- Liên hệ kiến thức tham khảo với dự án thực tế đang xây dựng bằng ASP.NET Core .NET 8.

---

## 2. Nhóm nguồn học thuật

## 2.1. IEEE Xplore

URL: https://ieeexplore.ieee.org

### Nội dung có thể tham khảo

IEEE Xplore là thư viện số chuyên về các bài báo khoa học trong lĩnh vực kỹ thuật, công nghệ thông tin, hệ thống phần mềm, mạng máy tính, cơ sở dữ liệu và trí tuệ nhân tạo.

Đối với đề tài CinemaBD, IEEE Xplore có thể dùng để tìm các nhóm tài liệu:

- Software architecture.
- Web-based management system.
- Online booking system.
- Information system design.
- Database management system.
- Secure payment system.
- Real-time web application.

### Cách áp dụng vào đề tài

Trong báo cáo, IEEE Xplore có thể được dùng để củng cố phần:

- Cơ sở lý thuyết về hệ thống thông tin quản lý.
- Kiến trúc phần mềm nhiều tầng.
- Thiết kế hệ thống đặt vé trực tuyến.
- Bảo mật và thanh toán trực tuyến.

### Gợi ý từ khóa tìm kiếm

```text
cinema management system
movie ticket booking system
online booking system architecture
web based management system
software architecture layered architecture
real time seat reservation system
```

---

## 2.2. ACM Digital Library

URL: https://dl.acm.org

### Nội dung có thể tham khảo

ACM Digital Library là nguồn tài liệu học thuật lớn trong ngành khoa học máy tính. Nguồn này phù hợp để tìm hiểu về:

- Thiết kế hệ thống phần mềm.
- Cơ sở dữ liệu.
- Giao diện người dùng.
- Hệ thống web.
- Mô hình bảo mật.
- Trải nghiệm người dùng trong các hệ thống trực tuyến.

### Cách áp dụng vào đề tài

Có thể dùng ACM để bổ sung lý thuyết cho các phần:

- Thiết kế hệ thống web quản lý rạp phim.
- Mô hình đặt vé trực tuyến.
- Tổ chức cơ sở dữ liệu cho hệ thống giao dịch.
- Thiết kế giao diện người dùng.

### Gợi ý từ khóa tìm kiếm

```text
online ticket booking system
cinema information system
web application architecture
transaction processing system
user experience online booking
```

---

## 2.3. arXiv Computer Science

URL: https://arxiv.org/archive/cs

### Nội dung có thể tham khảo

arXiv là kho bài báo mở, có nhiều chủ đề thuộc khoa học máy tính. Trang Computer Science của arXiv phân loại tài liệu theo nhiều nhóm như:

- Artificial Intelligence.
- Software Engineering.
- Databases.
- Distributed, Parallel, and Cluster Computing.
- Human-Computer Interaction.
- Cryptography and Security.

### Cách áp dụng vào đề tài

Đối với CinemaBD, arXiv phù hợp để tìm các tài liệu mở về:

- Software Engineering.
- Database design.
- Recommender system cho phim.
- Web application security.
- Real-time system.

### Gợi ý từ khóa tìm kiếm

```text
movie recommendation system
web application security
software engineering layered architecture
database design online booking
real-time reservation system
```

---

## 2.4. Google Scholar

URL: https://scholar.google.com

### Nội dung có thể tham khảo

Google Scholar hỗ trợ tìm kiếm bài báo, luận văn, sách và trích dẫn học thuật. Đây là công cụ thuận tiện để tìm tài liệu liên quan đến đề tài bằng tiếng Anh hoặc tiếng Việt.

### Cách áp dụng vào đề tài

Có thể dùng Google Scholar để tìm nhanh các tài liệu về:

- Cinema management system.
- Online movie ticket booking.
- Web-based information system.
- Management information system.
- Three-tier architecture.
- E-payment integration.

### Gợi ý từ khóa tìm kiếm

```text
"cinema management system"
"online movie ticket booking system"
"movie ticket reservation system"
"web based cinema management system"
"three tier architecture" "online booking"
```

---

## 3. Nhóm nguồn kỹ thuật và kiến trúc phần mềm

## 3.1. Martin Fowler - Software Architecture

URL: https://martinfowler.com/architecture/

### Nội dung học được

Martin Fowler định nghĩa kiến trúc phần mềm là những phần quan trọng của hệ thống cần được kiểm soát tốt. Kiến trúc không chỉ là sơ đồ cấp cao mà còn là các quyết định thiết kế ảnh hưởng lâu dài đến khả năng phát triển, bảo trì và mở rộng phần mềm.

Một ý chính có thể áp dụng:

> Kiến trúc tốt giúp hệ thống dễ phát triển thêm chức năng trong tương lai, giảm chi phí sửa đổi và tránh tích tụ mã khó bảo trì.

### Áp dụng vào CinemaBD

Dự án CinemaBD chọn kiến trúc phân tầng vì:

- Dễ tách biệt giao diện, API, nghiệp vụ và dữ liệu.
- Dễ mở rộng thêm chức năng như voucher, tích điểm, QR check-in, dashboard realtime.
- Giảm việc controller xử lý quá nhiều logic.
- Giúp source code rõ ràng hơn cho đồ án.

Liên hệ trong dự án:

```text
CinemaBD.Domain
CinemaBD.Application
CinemaBD.Infrastructure
CinemaBD.Api
CinemaBD.Web
```

---

## 3.2. Refactoring.Guru - Design Patterns

URL: https://refactoring.guru/design-patterns

### Nội dung học được

Design pattern là các giải pháp mẫu cho những vấn đề thường gặp trong thiết kế phần mềm. Pattern không phải là đoạn code cố định, mà là bản thiết kế có thể điều chỉnh để giải quyết vấn đề trong từng ngữ cảnh cụ thể.

Refactoring.Guru chia design pattern thành các nhóm chính:

- Creational patterns.
- Structural patterns.
- Behavioral patterns.

### Áp dụng vào CinemaBD

Trong CinemaBD có thể liên hệ các pattern sau:

| Pattern/Nguyên tắc | Cách áp dụng trong dự án |
|---|---|
| Dependency Injection | Inject service vào controller qua interface |
| Repository/Service style | Service xử lý nghiệp vụ, controller chỉ điều hướng |
| DTO Pattern | API dùng request/response DTO trong thư mục `Contracts` |
| Facade | `CinemaApiClient` đóng vai trò lớp gọi API đơn giản cho Web |
| Separation of Concerns | Tách Web, API, Application, Domain, Infrastructure |

Ví dụ thực tế:

```csharp
services.AddScoped<IMovieService, MovieService>();
services.AddScoped<IBookingService, BookingService>();
services.AddScoped<IPaymentService, PaymentService>();
```

---

## 3.3. Awesome Software Architecture

URL: https://github.com/mehdihadeli/awesome-software-architecture

### Nội dung học được

Đây là danh sách tổng hợp tài liệu về kiến trúc phần mềm, bao gồm:

- Microservices.
- Communication patterns.
- API Gateway.
- Observability.
- Logging.
- Monitoring.
- Testing.
- Service boundaries.

### Áp dụng vào CinemaBD

Dự án hiện tại dùng kiến trúc phân tầng, chưa cần microservices vì phạm vi đồ án vừa phải. Tuy nhiên có thể học được các hướng phát triển:

- Tách service rõ ràng theo bounded context: phim, đặt vé, thanh toán, khách hàng, admin.
- Bổ sung logging/monitoring khi triển khai thật.
- Dùng health check cho API/database.
- Có thể tách API Gateway nếu sau này chia thành nhiều service.

Hướng phát triển tương lai:

```text
Cinema Service
Booking Service
Payment Service
Customer Service
Notification Service
Admin Service
```

---

## 3.4. InfoQ - Architecture & Design

URL: https://www.infoq.com/architecture-design/

### Nội dung học được

InfoQ cung cấp nhiều bài viết và bài trình bày về kiến trúc, thiết kế phần mềm, DevOps, cloud, container, automation, continuous delivery và observability.

Các chủ đề phù hợp với CinemaBD:

- Container.
- DevOps.
- Continuous Delivery.
- Infrastructure.
- Observability.
- Cloud deployment.

### Áp dụng vào CinemaBD

Dự án đã có Docker Compose để chạy:

```text
sqlserver
api
web
```

Có thể đưa vào báo cáo như một bước chuẩn hóa triển khai:

- Người khác tải source về có thể chạy bằng Docker.
- Giảm lỗi do khác môi trường cài đặt.
- Dễ demo trước hội đồng.
- Có thể phát triển tiếp CI/CD build-test-deploy.

---

## 4. Nhóm tài liệu hệ thống đặt vé xem phim

Khi tìm kiếm về hệ thống đặt vé xem phim, các tài liệu thường nhấn mạnh các thành phần sau:

## 4.1. Chức năng cốt lõi

Một hệ thống đặt vé xem phim thường cần:

- Quản lý phim.
- Quản lý rạp/phòng chiếu.
- Quản lý ghế.
- Quản lý suất chiếu.
- Tìm kiếm phim.
- Chọn ghế.
- Đặt vé.
- Thanh toán.
- Gửi vé điện tử.
- Quản trị doanh thu.

CinemaBD đã áp dụng các chức năng này vào các module:

```text
Movie
Showtime
Room
Seat
Booking
Invoice
Payment
Combo
Review
Statistics
```

## 4.2. Vấn đề cần chú ý trong hệ thống đặt vé

Các hệ thống đặt vé có một số vấn đề quan trọng:

### Tránh đặt trùng ghế

Nhiều người dùng có thể cùng chọn một ghế. Hệ thống cần kiểm tra trạng thái ghế tại thời điểm checkout và nên có cơ chế giữ ghế tạm thời.

CinemaBD liên hệ:

- Có sơ đồ ghế.
- Có trạng thái ghế.
- Có SignalR để cập nhật realtime.
- Có thể phát triển thêm timeout giữ ghế.

### Thanh toán an toàn

Thanh toán cần có callback, chữ ký giao dịch và kiểm tra trạng thái hóa đơn.

CinemaBD liên hệ:

- Có `VnPayUrlBuilder`.
- Có `VnPaySignatureValidator`.
- Có `PaymentService`.

### Quản trị dữ liệu

Rạp phim cần quản lý phim, suất chiếu, phòng, ghế, combo, hóa đơn và thống kê.

CinemaBD liên hệ:

- Có khu vực admin riêng.
- Có dashboard và thống kê.
- Có các controller admin ở Web và API.

---

## 5. Cách đưa vào báo cáo đồ án

Có thể bổ sung vào chương 2 và chương 3 của báo cáo như sau:

## 5.1. Chương 2 - Cơ sở lý thuyết

Thêm các mục:

```text
2.x. Hệ thống quản lý rạp phim
2.x. Kiến trúc phần mềm phân tầng
2.x. Design Pattern trong phát triển web
2.x. REST API và DTO
2.x. Docker trong triển khai ứng dụng web
```

Nội dung mẫu:

> Theo các tài liệu về kiến trúc phần mềm, một hệ thống dễ bảo trì cần tách biệt rõ các thành phần quan trọng. Vì vậy, đề tài CinemaBD áp dụng kiến trúc phân tầng gồm Domain, Application, Infrastructure, API và Web. Cách tổ chức này giúp giảm phụ thuộc giữa controller và database, đồng thời hỗ trợ mở rộng thêm các chức năng như voucher, tích điểm, realtime dashboard và thanh toán trực tuyến.

## 5.2. Chương 3 - Phân tích thiết kế hệ thống

Thêm phần liên hệ từ hệ thống đặt vé thực tế:

```text
- Quản lý phim tương ứng Movie module.
- Quản lý phòng/ghế tương ứng Room/Seat module.
- Đặt vé tương ứng Booking/Invoice module.
- Thanh toán tương ứng Payment module.
- Quản trị tương ứng Admin module.
- Realtime ghế tương ứng SignalR SeatHub.
```

---

## 6. Bảng tổng hợp nguồn tham khảo

| STT | Nguồn | Nội dung chính | Ứng dụng vào CinemaBD |
|---:|---|---|---|
| 1 | IEEE Xplore | Bài báo kỹ thuật, hệ thống phần mềm, bảo mật, database | Tìm bài về web-based management system, online booking |
| 2 | ACM Digital Library | Tài liệu khoa học máy tính, UX, software design | Bổ sung cơ sở lý thuyết về hệ thống web |
| 3 | arXiv CS | Bài báo mở về CS, software engineering, database, security | Tìm tài liệu về realtime, security, recommendation |
| 4 | Google Scholar | Công cụ tìm bài báo/luận văn | Tìm tài liệu bằng từ khóa chính xác |
| 5 | Martin Fowler | Kiến trúc phần mềm, quyết định thiết kế quan trọng | Giải thích vì sao chọn kiến trúc phân tầng |
| 6 | Refactoring.Guru | Design pattern, nguyên tắc thiết kế | Liên hệ DI, DTO, service layer, separation of concerns |
| 7 | Awesome Software Architecture | Tổng hợp kiến trúc, microservices, observability | Hướng phát triển monitoring, API Gateway, service boundary |
| 8 | InfoQ Architecture Design | DevOps, container, cloud, architecture | Liên hệ Docker, CI/CD, triển khai production |

---

## 7. Tài liệu tham khảo đề xuất đưa vào báo cáo

Có thể ghi trong phần tài liệu tham khảo:

```text
[1] IEEE Xplore, "Digital Library for Engineering and Technology", https://ieeexplore.ieee.org, truy cập ngày 20/05/2026.

[2] ACM Digital Library, "Computer Science Research Publications", https://dl.acm.org, truy cập ngày 20/05/2026.

[3] arXiv, "Computer Science Archive", https://arxiv.org/archive/cs, truy cập ngày 20/05/2026.

[4] Google Scholar, "Academic Search Engine", https://scholar.google.com, truy cập ngày 20/05/2026.

[5] Martin Fowler, "Software Architecture Guide", https://martinfowler.com/architecture/, truy cập ngày 20/05/2026.

[6] Refactoring.Guru, "Design Patterns", https://refactoring.guru/design-patterns, truy cập ngày 20/05/2026.

[7] Mehdihadeli, "Awesome Software Architecture", GitHub Repository, https://github.com/mehdihadeli/awesome-software-architecture, truy cập ngày 20/05/2026.

[8] InfoQ, "Software Architecture and Design", https://www.infoq.com/architecture-design/, truy cập ngày 20/05/2026.
```

---

## 8. Kết luận nghiên cứu

Sau khi tham khảo các nguồn học thuật và kỹ thuật, kiến trúc phù hợp cho đề tài **Xây dựng hệ thống quản lý rạp phim Bình Dương Cinema** là kiến trúc phân tầng kết hợp REST API và Docker deployment.

Lý do chọn:

- Phù hợp quy mô đồ án.
- Dễ giải thích trong báo cáo.
- Dễ triển khai bằng ASP.NET Core .NET 8.
- Dễ mở rộng thêm module mới.
- Phù hợp hệ thống quản lý có nhiều nghiệp vụ như phim, suất chiếu, ghế, đặt vé, thanh toán, khách hàng và thống kê.

Định hướng tiếp theo:

- Bổ sung nội dung này vào chương 2 của báo cáo.
- Dùng phần kiến trúc phân tầng đã thiết kế để đưa vào chương 3.
- Dùng danh sách tài liệu tham khảo ở mục 7 cho cuối báo cáo.
