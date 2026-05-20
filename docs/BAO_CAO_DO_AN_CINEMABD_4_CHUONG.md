# BÁO CÁO ĐỒ ÁN

## Đề tài: Xây dựng website đặt vé xem phim CinemaBD

**Sinh viên thực hiện:** ........................................  
**Mã số sinh viên:** ........................................  
**Lớp:** ........................................  
**Giảng viên hướng dẫn:** ........................................  
**Thời gian:** 2026

---

## MỤC LỤC

- [CHƯƠNG 1. TỔNG QUAN ĐỀ TÀI](#chương-1-tổng-quan-đề-tài)
- [CHƯƠNG 2. CƠ SỞ LÝ THUYẾT VÀ CÔNG NGHỆ SỬ DỤNG](#chương-2-cơ-sở-lý-thuyết-và-công-nghệ-sử-dụng)
- [CHƯƠNG 3. PHÂN TÍCH, THIẾT KẾ VÀ XÂY DỰNG HỆ THỐNG](#chương-3-phân-tích-thiết-kế-và-xây-dựng-hệ-thống)
- [CHƯƠNG 4. TRIỂN KHAI, KIỂM THỬ VÀ KẾT LUẬN](#chương-4-triển-khai-kiểm-thử-và-kết-luận)

---

# CHƯƠNG 1. TỔNG QUAN ĐỀ TÀI

## 1.1. Lý do chọn đề tài

Ngày nay, nhu cầu giải trí tại rạp chiếu phim ngày càng tăng. Người dùng thường có xu hướng tìm kiếm thông tin phim, xem lịch chiếu, chọn ghế và thanh toán vé trực tuyến thay vì mua vé trực tiếp tại quầy. Việc xây dựng một hệ thống đặt vé xem phim trực tuyến giúp rạp chiếu phim tối ưu quy trình phục vụ, giảm thời gian chờ đợi, tăng khả năng quản lý và nâng cao trải nghiệm khách hàng.

Đề tài **CinemaBD** được xây dựng nhằm mô phỏng một hệ thống website đặt vé xem phim hoàn chỉnh, bao gồm giao diện khách hàng, hệ thống quản trị, API backend, quản lý dữ liệu phim, suất chiếu, ghế, combo, hóa đơn, thanh toán và thống kê.

## 1.2. Mục tiêu của đề tài

Mục tiêu chính của đồ án là xây dựng website đặt vé xem phim có thể chạy được thực tế, dễ triển khai và dễ mở rộng. Các mục tiêu cụ thể gồm:

- Xây dựng giao diện người dùng để xem phim, xem chi tiết phim, chọn suất chiếu, chọn ghế và đặt vé.
- Xây dựng khu vực quản trị cho nhân viên/admin quản lý dữ liệu hệ thống.
- Xây dựng REST API phục vụ các nghiệp vụ chính.
- Áp dụng kiến trúc phân lớp để tách biệt nghiệp vụ, dữ liệu và giao diện.
- Tích hợp thanh toán demo qua VNPAY/Momo.
- Tích hợp gửi email, xuất vé PDF/QR.
- Cập nhật trạng thái ghế theo thời gian thực bằng SignalR.
- Hỗ trợ triển khai bằng Docker để người khác dễ tải về và chạy.

## 1.3. Phạm vi đề tài

Trong phạm vi đồ án, hệ thống tập trung vào các chức năng chính sau:

### Đối với khách hàng

- Đăng ký, đăng nhập tài khoản.
- Xem danh sách phim.
- Xem chi tiết phim.
- Xem lịch chiếu.
- Chọn ghế theo sơ đồ phòng chiếu.
- Chọn combo bắp nước.
- Thanh toán vé.
- Nhận thông tin vé, hóa đơn và mã QR.
- Đánh giá phim.

### Đối với quản trị viên/nhân viên

- Đăng nhập trang quản trị.
- Quản lý phim.
- Quản lý suất chiếu.
- Quản lý phòng chiếu và ghế.
- Quản lý khách hàng.
- Quản lý nhân viên và vai trò.
- Quản lý combo.
- Quản lý hóa đơn.
- Quản lý đánh giá.
- Thống kê doanh thu.
- Theo dõi dashboard quản trị.

## 1.4. Đối tượng sử dụng

Hệ thống hướng đến ba nhóm người dùng chính:

| Nhóm người dùng | Mô tả |
|---|---|
| Khách hàng | Người truy cập website để xem phim, đặt vé, thanh toán và nhận vé |
| Nhân viên | Người hỗ trợ quản lý lịch chiếu, vé, khách hàng, hóa đơn |
| Quản trị viên | Người quản lý toàn bộ dữ liệu, phân quyền, thống kê và vận hành hệ thống |

## 1.5. Ý nghĩa thực tiễn

Đề tài có ý nghĩa thực tiễn trong việc áp dụng công nghệ web hiện đại vào bài toán quản lý rạp chiếu phim. Hệ thống giúp sinh viên hiểu rõ quy trình phân tích, thiết kế, lập trình, kiểm thử và triển khai một ứng dụng web hoàn chỉnh. Ngoài ra, việc sử dụng Docker giúp dự án dễ cài đặt trên nhiều máy khác nhau, phù hợp cho việc nộp đồ án, demo hoặc phát triển tiếp.

---

# CHƯƠNG 2. CƠ SỞ LÝ THUYẾT VÀ CÔNG NGHỆ SỬ DỤNG

## 2.1. ASP.NET Core MVC

ASP.NET Core MVC là framework dùng để xây dựng ứng dụng web theo mô hình Model - View - Controller. Trong dự án CinemaBD, phần `CinemaBD.Web` sử dụng ASP.NET Core MVC/Razor để xây dựng giao diện khách hàng và giao diện quản trị.

Mô hình MVC gồm:

- **Model:** biểu diễn dữ liệu và đối tượng nghiệp vụ.
- **View:** hiển thị giao diện người dùng.
- **Controller:** tiếp nhận request, xử lý luồng nghiệp vụ và trả về view hoặc dữ liệu.

Ưu điểm của ASP.NET Core MVC:

- Tách biệt giao diện và xử lý.
- Dễ bảo trì và mở rộng.
- Hỗ trợ routing, session, authentication, dependency injection.
- Phù hợp xây dựng website có nhiều chức năng quản trị.

## 2.2. ASP.NET Core Web API

ASP.NET Core Web API được dùng để xây dựng các endpoint REST. Trong CinemaBD, project `CinemaBD.Api` cung cấp API cho các chức năng như đăng nhập, phim, đặt vé, thanh toán, đánh giá và các module admin.

Một số API controller tiêu biểu:

- `AuthController`
- `MoviesController`
- `BookingsController`
- `PaymentsController`
- `ReviewsController`
- `AdminMoviesController`
- `AdminShowtimesController`
- `AdminInvoicesController`
- `AdminStatisticsController`

Web API giúp hệ thống dễ mở rộng sang mobile app hoặc frontend khác trong tương lai.

## 2.3. Entity Framework Core

Entity Framework Core là ORM cho .NET, hỗ trợ thao tác dữ liệu thông qua object thay vì viết SQL thủ công ở mọi nơi. Trong dự án, EF Core được dùng ở tầng Infrastructure để kết nối SQL Server và SQLite.

Vai trò EF Core trong hệ thống:

- Khai báo DbContext.
- Truy vấn dữ liệu phim, suất chiếu, ghế, hóa đơn.
- Thực hiện migration cho web local database.
- Hỗ trợ seed dữ liệu ban đầu.

## 2.4. SQL Server và SQLite

Hệ thống sử dụng hai loại cơ sở dữ liệu:

- **SQL Server:** lưu dữ liệu nghiệp vụ chính của API, được seed từ file `CinemaBD.sql`.
- **SQLite:** dùng cho một phần dữ liệu local của web MVC.

Khi chạy bằng Docker, SQL Server được khởi tạo bằng container `mcr.microsoft.com/mssql/server:2022-latest`.

## 2.5. Kiến trúc phân lớp

CinemaBD được tổ chức theo kiến trúc phân lớp gồm:

```text
CinemaBD.Domain          Entity, enum, model nghiệp vụ
CinemaBD.Application     Interface, contract, use-case abstraction
CinemaBD.Infrastructure  EF Core, JWT, payment, service implementation
CinemaBD.Api             REST API controller
CinemaBD.Web             MVC/Razor giao diện người dùng
```

Quy tắc phụ thuộc:

```text
Api/Web -> Infrastructure -> Application -> Domain
```

Ý nghĩa của từng tầng:

| Tầng | Vai trò |
|---|---|
| Domain | Chứa entity và logic cốt lõi |
| Application | Chứa interface/service contract |
| Infrastructure | Cài đặt truy cập dữ liệu, JWT, payment, service |
| Api | Cung cấp REST endpoint |
| Web | Cung cấp giao diện người dùng |

Ưu điểm:

- Dễ bảo trì.
- Dễ kiểm thử.
- Giảm phụ thuộc trực tiếp giữa controller và database.
- Dễ thay đổi công nghệ lưu trữ hoặc giao diện.

## 2.6. JWT Authentication

JWT được sử dụng để xác thực API. Sau khi đăng nhập thành công, hệ thống sinh token chứa thông tin người dùng. Client gửi token trong header `Authorization` để truy cập các API cần xác thực.

Ưu điểm:

- Phù hợp cho REST API.
- Không cần lưu session phía server cho API.
- Dễ tích hợp với web/mobile.

## 2.7. SignalR

SignalR là thư viện hỗ trợ giao tiếp thời gian thực giữa server và client. Trong CinemaBD, SignalR được dùng cho module ghế, giúp cập nhật trạng thái ghế khi người dùng chọn hoặc khi ghế đã được đặt.

Ứng dụng thực tế:

- Hiển thị ghế đã đặt.
- Cảnh báo khi ghế vừa được người khác chọn.
- Hỗ trợ trải nghiệm realtime trong quá trình đặt vé.

## 2.8. Docker

Docker được dùng để đóng gói và chạy hệ thống trong container. CinemaBD có file `docker-compose.yml` để chạy:

- SQL Server
- API
- Web MVC

Lợi ích:

- Người khác tải về có thể chạy nhanh bằng `docker compose up -d --build`.
- Không cần cài đặt thủ công SQL Server hoặc cấu hình phức tạp.
- Dễ demo và triển khai.

---

# CHƯƠNG 3. PHÂN TÍCH, THIẾT KẾ VÀ XÂY DỰNG HỆ THỐNG

## 3.1. Khảo sát nghiệp vụ

Quy trình đặt vé xem phim thông thường gồm các bước:

1. Khách hàng truy cập website.
2. Khách hàng xem danh sách phim đang chiếu.
3. Khách hàng chọn phim và suất chiếu.
4. Hệ thống hiển thị sơ đồ ghế.
5. Khách hàng chọn ghế còn trống.
6. Khách hàng chọn combo nếu có nhu cầu.
7. Khách hàng tiến hành thanh toán.
8. Hệ thống ghi nhận hóa đơn, cập nhật ghế đã đặt.
9. Khách hàng nhận vé điện tử/QR/PDF/email.
10. Admin theo dõi doanh thu và quản lý dữ liệu.

## 3.2. Yêu cầu chức năng

### 3.2.1. Chức năng khách hàng

| STT | Chức năng | Mô tả |
|---|---|---|
| 1 | Đăng ký | Tạo tài khoản khách hàng |
| 2 | Đăng nhập | Đăng nhập để đặt vé và quản lý thông tin |
| 3 | Xem phim | Xem danh sách phim đang chiếu |
| 4 | Xem chi tiết | Xem mô tả, thời lượng, lịch chiếu, đánh giá |
| 5 | Chọn suất chiếu | Chọn ngày, giờ và phòng chiếu |
| 6 | Chọn ghế | Chọn ghế theo sơ đồ phòng |
| 7 | Chọn combo | Chọn bắp, nước hoặc combo đi kèm |
| 8 | Thanh toán | Thanh toán qua cổng thanh toán demo |
| 9 | Nhận vé | Nhận thông tin vé, mã QR, hóa đơn |
| 10 | Đánh giá phim | Gửi đánh giá sau khi xem phim |

### 3.2.2. Chức năng quản trị

| STT | Chức năng | Mô tả |
|---|---|---|
| 1 | Dashboard | Xem tổng quan doanh thu, vé bán, dữ liệu hệ thống |
| 2 | Quản lý phim | Thêm, sửa, xóa, xem danh sách phim |
| 3 | Quản lý suất chiếu | Tạo và cập nhật lịch chiếu |
| 4 | Quản lý phòng | Quản lý phòng chiếu |
| 5 | Quản lý ghế | Quản lý sơ đồ ghế, trạng thái ghế |
| 6 | Quản lý combo | Quản lý combo bắp nước |
| 7 | Quản lý hóa đơn | Theo dõi giao dịch và hóa đơn |
| 8 | Quản lý khách hàng | Xem và cập nhật thông tin khách hàng |
| 9 | Quản lý nhân viên | Quản lý nhân viên và vai trò |
| 10 | Thống kê | Xem doanh thu, vé bán, phim bán chạy |
| 11 | Voucher | Quản lý mã giảm giá |
| 12 | Tích điểm | Quản lý điểm thành viên |

## 3.3. Yêu cầu phi chức năng

- Giao diện dễ sử dụng, phù hợp với người dùng phổ thông.
- Hệ thống có khả năng chạy local và Docker.
- Code được tổ chức theo kiến trúc phân lớp.
- Dữ liệu nhạy cảm được cấu hình qua biến môi trường.
- API có Swagger để kiểm thử.
- Hệ thống có khả năng mở rộng thêm mobile app hoặc frontend riêng.

## 3.4. Tác nhân hệ thống

Các tác nhân chính:

- **Khách vãng lai:** xem phim, xem chi tiết, đăng ký.
- **Khách hàng:** đăng nhập, đặt vé, thanh toán, đánh giá.
- **Nhân viên:** quản lý một số nghiệp vụ rạp.
- **Admin:** quản trị toàn bộ hệ thống.

## 3.5. Use case tổng quát

### Use case khách hàng đặt vé

```text
Khách hàng
   -> Đăng nhập
   -> Chọn phim
   -> Chọn suất chiếu
   -> Chọn ghế
   -> Chọn combo
   -> Thanh toán
   -> Nhận vé
```

### Use case admin quản lý phim

```text
Admin
   -> Đăng nhập trang quản trị
   -> Vào module quản lý phim
   -> Thêm/Sửa/Xóa phim
   -> Cập nhật thông tin phim
   -> Lưu dữ liệu
```

### Use case admin xem thống kê

```text
Admin
   -> Đăng nhập
   -> Vào Dashboard/Thống kê
   -> Xem doanh thu
   -> Xem số vé bán
   -> Xem phim bán chạy
```

## 3.6. Thiết kế cơ sở dữ liệu

Dựa trên nghiệp vụ, hệ thống có các nhóm bảng chính:

### Nhóm người dùng và phân quyền

- Khách hàng
- Nhân viên
- Vai trò
- Tài khoản đăng nhập

### Nhóm phim và rạp

- Phim
- Thể loại
- Rạp
- Phòng chiếu
- Ghế
- Suất chiếu

### Nhóm đặt vé

- Đặt vé
- Chi tiết vé
- Hóa đơn
- Thanh toán
- Combo
- Chi tiết combo

### Nhóm tương tác và vận hành

- Đánh giá
- Voucher
- Tích điểm
- Thống kê
- Bài viết/sự kiện

Một số quan hệ tiêu biểu:

- Một phim có nhiều suất chiếu.
- Một phòng có nhiều ghế.
- Một suất chiếu thuộc một phim và một phòng.
- Một khách hàng có nhiều hóa đơn.
- Một hóa đơn có nhiều vé/ghế.
- Một phim có nhiều đánh giá.

## 3.7. Thiết kế kiến trúc hệ thống

Kiến trúc tổng thể:

```text
Người dùng
   |
   v
CinemaBD.Web  <------>  CinemaBD.Api
   |                         |
   |                         v
   |                  CinemaBD.Infrastructure
   |                         |
   v                         v
SQLite local          SQL Server CinemaBD
```

Mô tả:

- Người dùng thao tác qua website `CinemaBD.Web`.
- Web gọi API qua `CinemaApiClient` ở một số nghiệp vụ.
- API xử lý request thông qua controller.
- Controller gọi service interface.
- Service implementation nằm ở Infrastructure.
- Infrastructure thao tác với database SQL Server.
- Web cũng dùng SQLite cho một phần dữ liệu local/demo.

## 3.8. Thiết kế giao diện

Giao diện hệ thống chia làm hai khu vực:

### Giao diện khách hàng

- Trang chủ.
- Danh sách phim.
- Chi tiết phim.
- Chọn ghế.
- Chọn combo.
- Thanh toán.
- Kết quả đặt vé.

### Giao diện admin

- Dashboard.
- Quản lý phim.
- Quản lý suất chiếu.
- Quản lý phòng/ghế.
- Quản lý combo.
- Quản lý hóa đơn.
- Quản lý khách hàng.
- Quản lý nhân viên/vai trò.
- Quản lý đánh giá.
- Thống kê.

## 3.9. Xây dựng các chức năng chính

### 3.9.1. Chức năng xem phim

Người dùng truy cập trang chủ để xem danh sách phim. Hệ thống lấy dữ liệu phim từ database và hiển thị thông tin cơ bản như tên phim, hình ảnh, thời lượng, thể loại và trạng thái.

### 3.9.2. Chức năng chọn ghế

Khi khách hàng chọn suất chiếu, hệ thống hiển thị sơ đồ ghế theo phòng chiếu. Ghế được phân loại theo trạng thái:

- Ghế thường
- Ghế VIP
- Ghế đang chọn
- Ghế đã đặt
- Ghế bảo trì

Việc đồng bộ giao diện chọn ghế giữa khách hàng và admin giúp hệ thống trực quan, dễ sử dụng hơn.

### 3.9.3. Chức năng đặt vé

Sau khi chọn ghế, khách hàng có thể chọn combo và chuyển sang bước thanh toán. Khi thanh toán thành công, hệ thống tạo hóa đơn, cập nhật trạng thái ghế và sinh thông tin vé.

### 3.9.4. Chức năng thanh toán

Hệ thống tích hợp thanh toán demo qua VNPAY/Momo. Luồng xử lý cơ bản:

1. Khách hàng xác nhận đơn đặt vé.
2. Hệ thống tạo URL thanh toán.
3. Khách hàng thanh toán trên cổng thanh toán.
4. Cổng thanh toán chuyển về trang callback.
5. Hệ thống xác nhận giao dịch và cập nhật hóa đơn.

### 3.9.5. Chức năng quản trị

Khu vực admin cho phép quản lý dữ liệu vận hành rạp. Các controller admin được tổ chức riêng trong `CinemaBD.Web/Areas/Admin` và `CinemaBD.Api/Controllers/Admin`.

### 3.9.6. Chức năng thống kê

Admin có thể xem doanh thu, số lượng vé, hóa đơn và các thống kê phục vụ quản lý. Đây là chức năng quan trọng giúp rạp nắm được tình hình kinh doanh.

---

# CHƯƠNG 4. TRIỂN KHAI, KIỂM THỬ VÀ KẾT LUẬN

## 4.1. Môi trường phát triển

Hệ thống được phát triển với các công cụ:

- Hệ điều hành: Windows
- IDE: Visual Studio/Visual Studio Code
- Ngôn ngữ: C#
- Framework: .NET 8
- Database: SQL Server, SQLite
- Container: Docker Desktop
- Source control: Git, GitHub

## 4.2. Cấu trúc triển khai Docker

Dự án hỗ trợ chạy bằng Docker Compose với 3 service:

| Service | Vai trò | Port |
|---|---|---|
| `sqlserver` | Cơ sở dữ liệu SQL Server | 1433 |
| `api` | ASP.NET Core Web API | 5188 |
| `web` | ASP.NET Core MVC Web | 7188 |

File cấu hình chính:

```text
docker-compose.yml
.env.example
CinemaBD.Api/Dockerfile
CinemaBD.Web/Dockerfile
```

## 4.3. Hướng dẫn cài đặt và chạy hệ thống bằng Docker

### Bước 1: Clone source code

```bash
git clone https://github.com/thanhthien20051006-source/CinemaBD.git
cd CinemaBD
```

### Bước 2: Tạo file môi trường

```bash
cp .env.example .env
```

Trên Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

### Bước 3: Build và chạy container

```bash
docker compose up -d --build
```

### Bước 4: Truy cập hệ thống

- Website: `http://localhost:7188`
- Swagger API: `http://localhost:5188/swagger`
- SQL Server: `localhost,1433`

## 4.4. Một số lệnh vận hành Docker

Xem log toàn hệ thống:

```bash
docker compose logs -f
```

Xem log API:

```bash
docker compose logs -f api
```

Xem log Web:

```bash
docker compose logs -f web
```

Dừng container:

```bash
docker compose down
```

Xóa dữ liệu và seed lại database:

```bash
docker compose down -v
docker compose up -d --build
```

## 4.5. Kiểm thử hệ thống

### 4.5.1. Kiểm thử build

Lệnh kiểm thử build:

```powershell
dotnet build D:\ALL_CNTT\CinemaBD\CinemaBD.WebApi.sln --no-restore
```

Kết quả gần nhất:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

### 4.5.2. Kiểm thử Docker compose config

Lệnh kiểm tra cấu hình Docker Compose:

```bash
docker compose config --quiet
```

Kết quả:

```text
docker compose config OK
```

### 4.5.3. Kiểm thử chức năng dự kiến

Các nhóm chức năng cần kiểm thử khi demo:

| STT | Chức năng | Kết quả mong đợi |
|---|---|---|
| 1 | Mở trang chủ | Hiển thị danh sách phim |
| 2 | Xem chi tiết phim | Hiển thị thông tin phim và lịch chiếu |
| 3 | Chọn ghế | Hiển thị sơ đồ ghế, chọn được ghế còn trống |
| 4 | Chọn combo | Thêm combo vào đơn đặt vé |
| 5 | Thanh toán | Tạo giao dịch và trả về kết quả |
| 6 | Admin đăng nhập | Vào được dashboard quản trị |
| 7 | Quản lý phim | Thêm/sửa/xóa hoặc xem danh sách phim |
| 8 | Quản lý suất chiếu | Tạo/cập nhật lịch chiếu |
| 9 | Quản lý hóa đơn | Xem được danh sách hóa đơn |
| 10 | Thống kê | Hiển thị dữ liệu thống kê |

## 4.6. Kết quả đạt được

Sau quá trình xây dựng, hệ thống đạt được các kết quả chính:

- Xây dựng được website đặt vé xem phim bằng ASP.NET Core .NET 8.
- Có giao diện khách hàng và giao diện quản trị.
- Có REST API riêng cho các nghiệp vụ chính.
- Tổ chức source code theo kiến trúc phân lớp.
- Có database seed dữ liệu mẫu.
- Có Docker Compose để triển khai nhanh.
- Có tích hợp thanh toán demo, email, QR/PDF vé.
- Build solution thành công không lỗi.
- Source code đã được đưa lên GitHub.

## 4.7. Hạn chế

Một số hạn chế hiện tại:

- Một phần Web vẫn dùng SQLite local, chưa chuyển hoàn toàn sang API.
- Một số chức năng thanh toán cần cấu hình key thật hoặc public callback URL để chạy production.
- Chưa có đầy đủ unit test/integration test cho tất cả module.
- Chưa có CI/CD tự động.
- Một số giao diện có thể cần tối ưu thêm cho mobile.

## 4.8. Hướng phát triển

Trong tương lai, hệ thống có thể phát triển thêm:

- Chuyển Web sang gọi API hoàn toàn, giảm phụ thuộc SQLite local.
- Hoàn thiện giữ ghế realtime có timeout.
- Thêm IPN/callback thanh toán chuẩn production.
- Bổ sung voucher, tích điểm, hạng thành viên.
- Thêm chức năng quét QR vé cho nhân viên soát vé.
- Bổ sung dashboard realtime.
- Xuất báo cáo Excel/PDF.
- Thêm unit test và integration test.
- Thiết lập CI/CD tự động build, test, deploy.
- Triển khai production với domain, HTTPS, reverse proxy và backup database.

## 4.9. Kết luận

Đồ án CinemaBD đã xây dựng được một hệ thống website đặt vé xem phim tương đối đầy đủ, bao gồm giao diện người dùng, khu vực quản trị, API backend, database, thanh toán demo và triển khai Docker. Thông qua đồ án, sinh viên nắm được quy trình phát triển phần mềm web từ phân tích yêu cầu, thiết kế kiến trúc, xây dựng chức năng, kiểm thử đến triển khai.

Hệ thống vẫn còn khả năng mở rộng và hoàn thiện thêm, tuy nhiên phiên bản hiện tại đã đáp ứng được các nghiệp vụ chính của một website đặt vé xem phim và có thể dùng để demo, học tập hoặc phát triển tiếp trong thực tế.

---

# TÀI LIỆU THAM KHẢO

1. Microsoft Docs - ASP.NET Core: https://learn.microsoft.com/aspnet/core
2. Microsoft Docs - Entity Framework Core: https://learn.microsoft.com/ef/core
3. Microsoft Docs - SignalR: https://learn.microsoft.com/aspnet/core/signalr
4. Docker Docs: https://docs.docker.com
5. SQL Server Documentation: https://learn.microsoft.com/sql/sql-server
