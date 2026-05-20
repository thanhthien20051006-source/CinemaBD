# HƯỚNG PHÁT TRIỂN CHỨC NĂNG CINEMABD

## Thứ tự ưu tiên triển khai

```text
1. Đồng bộ dữ liệu hóa đơn/vé/suất chiếu/phim
2. QR check-in theo từng vé
3. Vé PDF + gửi email có QR
4. Realtime giữ ghế backend thật
5. Dashboard doanh thu nâng cao
6. Voucher + điểm thưởng nâng cao
7. Hủy vé / hoàn tiền
8. Review phim
9. POS bán vé tại quầy
10. Multi-cinema
```

---

## 1. Đồng bộ dữ liệu hóa đơn/vé/suất chiếu/phim

### Mục tiêu
Đảm bảo sau khi thanh toán, dữ liệu luôn đồng bộ giữa:

```text
HoaDons
HoaDonChiTiet
VE
SUATCHIEU
PHIM
KHACHHANG
THANHTOAN
```

### Vấn đề hiện tại
Một số hóa đơn cũ có `HoaDons` nhưng thiếu `HoaDonChiTiet`, hoặc không truy ra được phim/ngày chiếu/giờ chiếu.

### Cần làm
- Chuẩn hóa luồng tạo hóa đơn sau thanh toán.
- Mỗi giao dịch thành công phải có:
  - 1 dòng `HoaDons`
  - nhiều dòng `HoaDonChiTiet`
  - vé trong `VE` chuyển sang `Paid`
  - combo đã đặt được lưu vào chi tiết hóa đơn
- Admin hóa đơn phải join được:

```text
HoaDons -> HoaDonChiTiet -> VE -> SUATCHIEU -> PHIM
```

### API cần ổn định
```http
GET /api/admin/invoices
GET /api/admin/invoices/{id}
GET /api/bookings/invoice/{txnRef}
GET /api/account/invoices
```

### Kết quả mong muốn
- Lịch sử hóa đơn user hiển thị đúng.
- Admin chi tiết hóa đơn có đủ vé/combo/phim/suất chiếu.
- QR check-in lấy đúng dữ liệu.

---

## 2. QR check-in theo từng vé

### Mục tiêu
Mỗi vé có QR riêng, không chỉ check-in theo hóa đơn.

### Cần làm
Thêm trạng thái check-in cho từng vé:

```sql
ALTER TABLE VE ADD DaCheckIn bit NULL;
ALTER TABLE VE ADD ThoiGianCheckIn datetime2 NULL;
```

### QR payload đề xuất
```text
CinemaBD|TICKET|<MaVe>|<Signature>
```

### Signature
Dùng HMAC để chống fake QR:

```csharp
HMACSHA256(MaVe + "|" + GatewayTxnRef, SecretKey)
```

### API cần thêm
```http
POST /api/admin/tickets/check-in
GET /api/admin/tickets/{maVe}
```

### Rule
- Vé phải tồn tại.
- Vé phải `Paid`.
- Vé chưa check-in.
- Suất chiếu hợp lệ.
- Check-in lần 2 phải bị chặn.

---

## 3. Vé PDF + gửi email có QR

### Mục tiêu
Sau thanh toán, user nhận email có hóa đơn + vé PDF + QR check-in.

### Cần làm
- Tạo PDF vé.
- Mỗi vé trong hóa đơn có 1 QR.
- Gửi email kèm file PDF.
- Cho phép user tải lại vé PDF.
- Cho phép admin/user gửi lại vé qua email.

### Thư viện đề xuất
```text
QuestPDF
```
hoặc:

```text
DinkToPdf
```

### API cần thêm
```http
GET /api/bookings/invoice/{txnRef}/pdf
POST /api/bookings/invoice/{txnRef}/resend-email
```

### UI cần thêm
Ở trang hóa đơn:

```text
Tải vé PDF
Gửi lại email
```

---

## 4. Realtime giữ ghế backend thật

### Mục tiêu
Không chỉ khóa ghế trên UI, mà khóa thật ở backend.

### Bảng đề xuất
```sql
CREATE TABLE SeatHolds (
    Id int identity primary key,
    MaSuatChieu nvarchar(50) not null,
    MaGhe nvarchar(50) not null,
    MaKH nvarchar(50) null,
    SessionId nvarchar(100) null,
    GatewayTxnRef nvarchar(100) null,
    HoldUntil datetime2 not null,
    Status nvarchar(30) not null,
    CreatedAt datetime2 not null
);
```

### Rule
- Chọn ghế -> tạo hold 10 phút.
- User khác không chọn được ghế đang hold.
- Hết hạn -> tự unlock.
- Thanh toán thành công -> chuyển vé sang `Paid`.
- Thanh toán thất bại/hết hạn -> unlock.

### SignalR event
```text
SeatHeld
SeatReleased
SeatPaid
SeatExpired
```

---

## 5. Dashboard doanh thu nâng cao

### Mục tiêu
Admin xem được tình hình kinh doanh rõ ràng.

### Chỉ số cần có
```text
Tổng doanh thu
Tổng vé bán
Tổng combo bán
Tỷ lệ lấp đầy ghế
Doanh thu theo ngày/tháng/năm
Doanh thu theo phim
Doanh thu theo phương thức thanh toán
Top phim bán chạy
Top khách hàng
Tỷ lệ check-in
Tỷ lệ hủy vé/refund
```

### API cần thêm
```http
GET /api/admin/statistics/revenue/summary
GET /api/admin/statistics/revenue/by-day
GET /api/admin/statistics/revenue/by-movie
GET /api/admin/statistics/revenue/by-payment-method
GET /api/admin/statistics/occupancy
```

### UI
Dùng chart:

```text
Chart.js
```

---

## 6. Voucher + điểm thưởng nâng cao

### Mục tiêu
Tạo hệ thống khuyến mãi và thành viên thân thiết.

### Voucher cần hỗ trợ
```text
Giảm theo phần trăm
Giảm số tiền cố định
Giới hạn số lượt dùng
Giới hạn theo user
Giới hạn theo phim
Giới hạn theo ngày
Giá trị đơn tối thiểu
Mức giảm tối đa
Voucher sinh nhật
Voucher khách hàng mới
```

### Field nên thêm cho Voucher
```text
DiscountType
DiscountValue
MinOrderAmount
MaxDiscountAmount
UsageLimit
UsedCount
StartDate
EndDate
TargetMovieId
TargetCustomerId
IsActive
```

### Điểm thưởng
Rule đề xuất:

```text
10.000đ = 1 điểm
100 điểm = giảm 10.000đ
```

### Bảng nên thêm
```text
LoyaltyTransactions
MembershipTiers
VoucherUsages
```

---

## 7. Hủy vé / hoàn tiền

### Mục tiêu
User có thể yêu cầu hủy vé, admin duyệt hoàn tiền.

### Trạng thái vé
```text
Pending
Paid
CancelRequested
Cancelled
Refunded
Expired
```

### Bảng đề xuất
```text
RefundRequests
```

Field:

```text
Id
MaHD
MaVe
MaKH
Reason
Status
RequestedAt
ApprovedAt
RejectedAt
RefundAmount
AdminNote
```

### Rule
- Không cho hủy nếu còn dưới 2 giờ trước suất chiếu.
- Nếu đã check-in thì không được hủy.
- Nếu dùng voucher/điểm thì xử lý hoàn phù hợp.

### API cần thêm
```http
POST /api/bookings/refund-request
GET /api/admin/refunds
POST /api/admin/refunds/{id}/approve
POST /api/admin/refunds/{id}/reject
```

---

## 8. Review phim

### Mục tiêu
User đã xem phim có thể đánh giá phim.

### Rule
- Chỉ user đã mua vé mới được review.
- Suất chiếu phải đã qua.
- Mỗi user chỉ review 1 lần cho 1 phim.
- Admin có quyền ẩn/xóa review.

### Bảng đề xuất
```text
Reviews
```

Field:

```text
Id
MaPhim
MaKH
MaHD
Rating
Content
Status
CreatedAt
UpdatedAt
```

### API
```http
GET /api/reviews/movie/{maPhim}
POST /api/reviews
DELETE /api/admin/reviews/{id}
```

---

## 9. POS bán vé tại quầy

### Mục tiêu
Nhân viên bán vé trực tiếp tại rạp.

### Chức năng
```text
Chọn phim
Chọn suất chiếu
Chọn ghế
Chọn combo
Thanh toán tiền mặt/chuyển khoản
In vé ngay
Gửi vé qua email nếu khách có tài khoản
```

### Phân quyền
```text
Admin
Quản lý rạp
Nhân viên bán vé
Nhân viên soát vé
```

### API cần thêm
```http
POST /api/admin/pos/checkout
GET /api/admin/pos/showtimes
GET /api/admin/pos/seats/{showtimeId}
```

### UI
Trang admin mới:

```text
/Admin/POS
```

---

## 10. Multi-cinema

### Mục tiêu
Hệ thống hỗ trợ nhiều rạp/chi nhánh.

### Bảng nên thêm
```text
Cinemas
Branches
```

Hoặc tối thiểu thêm vào `PHONGCHIEU`:

```text
MaRap
```

### Quan hệ dữ liệu
```text
Rap -> PhongChieu -> Ghe
Rap -> SuatChieu
SuatChieu -> Phim
```

### Chức năng
```text
User chọn rạp gần nhất
Lọc phim theo rạp
Lọc suất chiếu theo rạp
Dashboard doanh thu theo rạp
Admin quản lý phòng/ghế theo rạp
```

### API cần thêm
```http
GET /api/cinemas
GET /api/cinemas/{id}/movies
GET /api/cinemas/{id}/showtimes
GET /api/admin/cinemas
POST /api/admin/cinemas
```

---

## Roadmap triển khai đề xuất

### Giai đoạn 1 - Ổn định dữ liệu lõi
```text
1. Đồng bộ hóa đơn/vé/suất chiếu/phim
2. QR check-in theo từng vé
3. Vé PDF + gửi email QR
```

### Giai đoạn 2 - Realtime + vận hành
```text
4. Realtime giữ ghế backend thật
5. Dashboard doanh thu nâng cao
6. Voucher + điểm thưởng nâng cao
```

### Giai đoạn 3 - Nghiệp vụ thực tế
```text
7. Hủy vé / hoàn tiền
8. Review phim
9. POS bán vé tại quầy
```

### Giai đoạn 4 - Mở rộng quy mô
```text
10. Multi-cinema
```
