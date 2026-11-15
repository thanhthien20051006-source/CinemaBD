# TRIỂN KHAI GIAI ĐOẠN 1 - ỔN ĐỊNH NỀN TẢNG VÀ DỮ LIỆU DEMO

Thời gian bắt đầu: 20/05/2026 14:06 GMT+7

## 1. Mục tiêu giai đoạn 1

Theo kế hoạch phát triển dự án, giai đoạn 1 tập trung vào:

- Kiểm tra Git working tree.
- Kiểm tra build solution.
- Kiểm tra Docker Compose config.
- Kiểm tra Docker Desktop/Docker Engine.
- Kiểm tra file `.env.example`.
- Kiểm tra seed database và tài khoản demo.
- Xác định blocker trước khi chạy full Docker.

---

## 2. Kết quả kiểm tra hiện tại

## 2.1. Git

Repo hiện sạch, không có file thay đổi trước khi triển khai giai đoạn 1.

Lệnh:

```powershell
git status --short
```

Kết quả:

```text
Không có thay đổi chưa commit
```

## 2.2. Build solution

Lệnh:

```powershell
dotnet build CinemaBD.WebApi.sln --no-restore
```

Kết quả:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Các project build thành công:

```text
CinemaBD.Domain
CinemaBD.Application
CinemaBD.Infrastructure
CinemaBD.Web
CinemaBD.Api
```

## 2.3. Docker Compose config và image build

Ghi chú dưới đây là kết quả lịch sử tại thời điểm bắt đầu giai đoạn 1; không phải trạng thái hiện tại:

```powershell
docker compose config --quiet
```

Lần kiểm tra trước chưa thực hiện được do Docker Engine chưa chạy:

```text
failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine
The system cannot find the file specified.
```

Sau đó, chủ dự án báo đã chạy thành công:

```text
docker compose build api web
Kết quả: build thành công, 90,8 giây, không lỗi.
```

Kết quả build trên chỉ xác nhận hai image được build; không xác nhận `docker compose up`, trạng thái healthy của service hay khởi tạo/seed database.

Lệnh `docker compose config --quiet` và việc khởi chạy các container chưa được xác minh trong lần cập nhật này.

Lưu ý: lỗi lịch sử cho biết Docker CLI không kết nối được Docker Engine vào thời điểm kiểm tra đó; không có nghĩa Dockerfile không build được.
## 2.4. Docker CLI

Lệnh:

```powershell
docker version
```

Kết quả phần client có tồn tại:

```text
Client:
 Version: 29.4.0
 Context: desktop-linux
```

Nhưng server/engine chưa chạy.

## 2.5. File `.env.example`

File đã có các biến cần thiết:

```text
SA_PASSWORD
DB_NAME
JWT_KEY
JWT_ISSUER
JWT_AUDIENCE
WEB_PUBLIC_URL
VNPAY_BASE_URL
VNPAY_TMN_CODE
VNPAY_HASH_SECRET
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
SMTP_HOST
SMTP_PORT
SMTP_ENABLE_SSL
SMTP_FROM_NAME
SMTP_FROM_EMAIL
SMTP_PASSWORD
```

Các secret đều đang là placeholder, không dùng secret thật.

## 2.6. Tài khoản demo từ `CinemaBD.sql`

Dữ liệu seed hiện có tài khoản demo:

### Admin

```text
Username: admin
Password hash: 202cb962ac59075b964b07152d234b70
Mật khẩu tương ứng: 123
```

### Khách hàng

```text
Username: ttt123
Password hash: 202cb962ac59075b964b07152d234b70
Mật khẩu tương ứng: 123
```

Ngoài ra còn có tài khoản:

```text
Username: ttt1
Password hash: e10adc3949ba59abbe56e057f20f883e
Mật khẩu tương ứng: 123456
```

---

## 3. Trạng thái checklist giai đoạn 1

| STT | Hạng mục | Trạng thái | Ghi chú |
|---:|---|---|---|
| 1 | Git sạch | Đạt | Không có thay đổi trước triển khai |
| 2 | Build solution | Đạt | 0 warning, 0 error |
| 3 | `.env.example` | Đạt | Đủ biến, không có secret thật |
| 4 | Tài khoản demo | Đạt | Có admin/user trong seed SQL |
| 5 | Docker CLI | Đạt một phần | Có client Docker |
| 6 | Docker Engine | Chưa đạt | Docker Desktop chưa bật |
| 7 | Docker Compose config | Chưa kiểm được | Phụ thuộc Docker Engine |
| 8 | `docker compose up` | Chưa chạy được | Phụ thuộc Docker Engine |

---

## 4. Trạng thái còn cần xác minh

Lệnh `docker compose build api web` đã được báo thành công ở mục 2.3. Việc này không chứng minh Docker Compose config đã được kiểm tra, cũng không chứng minh các container đã khởi chạy hoặc ứng dụng kết nối/seed database thành công.

Nếu cần xác minh runtime, thực hiện riêng các bước sau khi Docker Engine sẵn sàng:

```powershell
docker compose config --quiet
docker compose up -d
docker compose ps
```

---

## 5. Việc tiếp theo sau khi Docker chạy

Sau khi bật Docker Desktop, chạy tiếp:

```powershell
cd D:\ALL_CNTT\CinemaBD
Copy-Item .env.example .env -ErrorAction SilentlyContinue
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs -f api
```

Sau đó kiểm tra:

```text
http://localhost:7188
http://localhost:5188/swagger
```

Test đăng nhập:

```text
Admin: admin / 123
User: ttt123 / 123
```

---

## 6. Kết luận và phạm vi bằng chứng

Build solution .NET và dữ liệu demo có kết quả như ghi ở trên. Chủ dự án báo `docker compose build api web` hoàn tất thành công trong 90,8 giây, không lỗi. Đây là bằng chứng build image API/Web, không phải bằng chứng Compose runtime.

`docker compose config --quiet`, khởi chạy/health của các service, và khởi tạo database trong container vẫn chưa được xác minh trong lần cập nhật này. Lỗi kết nối Docker Engine ở mục 2.3 là kết quả lịch sử tại thời điểm kiểm tra trước đó.
