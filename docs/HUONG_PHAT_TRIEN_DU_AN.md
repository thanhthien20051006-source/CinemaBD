# Hướng phát triển dự án CinemaBD

## 1. Hoàn thiện nghiệp vụ đặt vé
- Đồng bộ sơ đồ ghế khách hàng/admin theo phòng chiếu thực tế.
- Giữ ghế realtime bằng SignalR/WebSocket để nhiều người đặt cùng lúc thấy ghế đang được giữ.
- Tự hủy đơn quá hạn giữ ghế và trả ghế về trạng thái trống.
- Tách rõ trạng thái ghế: `Hoạt động`, `Bảo trì`, `Đã đặt`, `Đang giữ`.

## 2. Thanh toán
- Hoàn thiện VNPAY return + IPN public bằng ngrok/tunnel/domain thật.
- Thêm idempotency để VNPAY return/IPN không cộng điểm hoặc xác nhận thanh toán lặp.
- Chuẩn hóa MoMo sandbox/thật.
- Thêm trang tra cứu trạng thái giao dịch.

## 3. Vé điện tử
- QR vé có checksum/chữ ký để chống giả mã vé.
- Thêm endpoint kiểm tra vé cho nhân viên soát vé.
- Xuất PDF vé/hóa đơn.
- Gửi email vé có QR + PDF đính kèm.

## 4. Voucher và khuyến mãi
- Voucher theo phần trăm/số tiền cố định.
- Voucher giới hạn lượt dùng, ngày bắt đầu/kết thúc.
- Voucher theo khách hàng/hạng thành viên/phim/suất chiếu.
- Báo cáo hiệu quả voucher.

## 5. Tích điểm thành viên
- Dùng điểm để giảm tiền khi checkout.
- Hạng thành viên: Silver/Gold/Diamond.
- Lịch sử cộng/trừ điểm.
- Chống cộng điểm lặp theo mã giao dịch.

## 6. Admin vận hành
- Dashboard realtime: doanh thu ngày, vé bán, suất đang chiếu.
- Quản lý lịch chiếu bằng calendar view/kéo thả.
- Quản lý phòng chiếu trực quan bằng sơ đồ ghế.
- Phân quyền chi tiết theo chức năng.

## 7. Báo cáo - thống kê
- Doanh thu theo ngày/tháng/năm.
- Top phim bán chạy.
- Tỷ lệ lấp đầy phòng chiếu.
- Doanh thu theo combo/dịch vụ.
- Xuất Excel/PDF báo cáo.

## 8. Kiến trúc và dữ liệu
- Chuẩn hóa seed/migration Docker để reset DB không mất bảng `VOUCHER`, `TICHDIEM`, dữ liệu demo.
- Di chuyển Web local SQLite sang gọi API hoàn toàn.
- Tách DTO/API contract rõ ràng.
- Thêm unit/integration test cho booking/payment/voucher/loyalty.

## 9. UX/UI
- Responsive tốt hơn trên mobile.
- Trang thanh toán thành công đẹp hơn, có QR/PDF/email.
- Flow chọn phim → suất → ghế → combo → thanh toán mượt hơn.
- Thông báo realtime khi ghế vừa bị người khác giữ.

## 10. Triển khai thật
- Docker compose production.
- Reverse proxy Nginx/Caddy.
- HTTPS/domain thật.
- CI/CD build/test/deploy.
- Backup SQL Server định kỳ.
