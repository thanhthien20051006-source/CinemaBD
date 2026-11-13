# Nhật ký sự cố bảo mật / vận hành

> Ghi nhận dựa trên thông tin được cung cấp và cấu hình/mã nguồn hiện tại. Mốc SQL Server là giờ máy chủ do người vận hành báo; không diễn giải thành xác nhận nguyên nhân tuyệt đối nếu chưa có audit log/backup độc lập.

## Sự cố 1 — phát sinh 296 suất chiếu

- **Thời điểm:** Ngày/giờ cụ thể chưa được xác nhận trong hồ sơ đang có; ghi nhận phát hiện hiện tượng 296 suất chiếu ngoài dự kiến.
- **Timeline:** (1) API được khởi động với cấu hình initializer hiện hành; (2) `DatabaseInitializer` gọi `EnsureUpcomingShowtimesAsync` khi `DatabaseSeed:AllowDemoData` bật; (3) số lượng suất chiếu được người vận hành ghi nhận là 296. Không có mốc thời gian của từng bước để đối chiếu độc lập.
- **Nguyên nhân gốc:** Đường khởi tạo có thể tự sửa/tạo lịch suất chiếu; hành vi phụ thuộc cờ demo nhưng không có opt-in độc lập để cho phép mọi thao tác DB. Cơ chế cũng chạy tại startup, nên việc khởi chạy API có thể tạo thay đổi nghiệp vụ không dự kiến. Chưa có đủ bằng chứng để khẳng định chính xác phần nào trong 296 bản ghi được tạo mới hay sửa đổi.
- **Biện pháp:** Đặt `Database:AllowAutoMigrate` mặc định `false`, yêu cầu database name phải nằm chính xác trong `Database:AllowedNames`, và kiểm tra cả hai điều kiện trước mọi side effect của initializer. Tách cờ `ShowtimeExpiry:Enabled`, mặc định `false`. Khi xác nhận môi trường phát triển, chỉ bật các cờ cho đúng database đã chọn; trước thao tác sửa dữ liệu, kiểm kê và sao lưu theo quy trình vận hành.

## Sự cố 2 — B3 schema tự áp dụng lên `CinemaBD`

- **Thời điểm:** Theo người vận hành, bảng/schema B3 đã xuất hiện trên database `CinemaBD` lúc **2026-11-13 09:58:57 giờ SQL Server** (UTC+07:00 trên máy phát triển). Migration B3 trong repo là `20261112174026_B3SessionRefreshSchema`.
- **Timeline:** (1) B3 được thêm vào mã nguồn/migration; (2) API khởi động lúc gần thời điểm trên; (3) startup gọi `DatabaseInitializer.InitializeAsync`; (4) initializer gọi `Database.MigrateAsync()` nếu có migration pending. Người vận hành đối chiếu thời điểm bảng và báo migration được áp dụng lên `CinemaBD`.
- **Nguyên nhân gốc:** API mặc định chứa connection string trỏ thẳng tới database tên `CinemaBD`, còn initializer tự áp dụng pending migrations khi startup mà không yêu cầu allowlist hay cờ riêng để xác nhận đây là target được chấp thuận. Do vậy, chạy API có thể làm thay đổi database đích không được chọn tường minh cho migration.
- **Biện pháp:** Xóa tên database khỏi cấu hình API mặc định; bắt buộc cấu hình `ConnectionStrings__DefaultConnection`; cho phép initializer chỉ khi `Database__AllowAutoMigrate=true` và tên `Initial Catalog` có trong `Database__AllowedNames`. Mọi trường hợp thiếu điều kiện chỉ cảnh báo và bỏ qua. Không thực hiện migration, seed hoặc kiểm tra ghi lên database production như một phần của guard này; xác minh qua test không kết nối SQL Server.

## Trạng thái sau guard

Guard đóng vai trò kiểm soát cấu hình và không tự khôi phục dữ liệu đã thay đổi. Với từng sự cố, cần xác minh trạng thái DB từ backup/audit/deployment logs của môi trường liên quan, sau đó áp dụng quy trình restore/repair được phê duyệt riêng. Không coi các đề xuất phục hồi này là đã thực thi.
