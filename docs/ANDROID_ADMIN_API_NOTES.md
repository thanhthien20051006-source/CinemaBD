# CinemaBD Admin API — Android reference

> **Phạm vi:** các endpoint của admin trong `CinemaBD.Api/Controllers/Admin` thuộc 8 nhóm hóa đơn, thống kê, suất chiếu, phim, voucher, combo, nhân viên và hoàn tiền.
>
> **Đối chiếu tài liệu Android:** `docs/ANDROID_API_NOTES.md` mô tả API khách hàng và nêu rõ không bao gồm các controller `Admin`. Vì vậy tất cả endpoint dưới đây được đánh dấu **Chưa có** trong tài liệu đó. Không đánh dấu endpoint có sẵn chỉ vì cùng loại dữ liệu xuất hiện ở phần khách hàng (ví dụ danh sách phim/combo hoặc yêu cầu hoàn tiền).

## Quy ước chung

- Gọi endpoint tương đối trên API base URL, ví dụ `https://<host>/api/admin/movies`.
- Các route admin đầy đủ được xác định từ route attribute của từng controller. Policy `AdminOnly` yêu cầu JWT role `Admin`; ngoại lệ duy nhất trong admin controllers là đăng nhập `/api/admin/auth/login`, không gắn `[Authorize]`.
- `Envelope<T>` là `ApiResponse<T>`: `{ success, message, data }`. HTTP status và `success` cần được kiểm tra riêng.
- `AdminOnly` yêu cầu JWT hợp lệ có role `Admin`: `Authorization: Bearer <token>`. Policy được đăng ký bằng `RequireRole("Admin")`; JSON dùng camelCase theo ASP.NET Core web defaults (`AddControllers()` không tùy biến naming policy).
- `—` nghĩa là không có path/query/body input tương ứng. Tên trong `{...}` là path parameter; các trường JSON body được liệt kê bằng camelCase. `?` đánh dấu trường nullable/tùy chọn.
- `data` thường là DTO/entity được ghi tên và liệt kê trường. Các kiểu `object` được ghi là kiểu cụ thể được trả về khi có thể truy vết; không coi một tên kiểu `object` là schema rỗng.

## Endpoint quản trị ngoài 8 nhóm chính

Để đáp ứng phạm vi **toàn bộ** `CinemaBD.Api/Controllers/Admin`, phần này liệt kê các controller admin còn lại. Tất cả các endpoint này yêu cầu `AdminOnly` trừ route login ở nhóm xác thực. Dữ liệu trả về trực tiếp từ entity/service được ghi là kiểu entity/service; JSON dùng camelCase. Vì `docs/ANDROID_API_NOTES.md` loại trừ các controller admin, các endpoint dưới đây cũng đều **Chưa có** trong tài liệu đó.

### Xác thực admin

| Method | Route | Quyền | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|---|
| POST | `/api/admin/auth/login` | Công khai (không `[Authorize]`) | JSON `AdminLoginRequest`: `{ username, password }` | `AdminAuthResponse`: `adminId`, `username`, `fullName`, `role`, `token` | **Chưa có** |

### Bài viết

Controller `/api/admin/articles` — `AdminOnly`. `Article` fields: `id`, `title`, `summary?`, `content?`, `imageUrl?`, `publishedAt`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/articles` | — | `Article[]`: `id`, `title`, `summary?`, `content?`, `imageUrl?`, `publishedAt` | **Chưa có** |
| GET | `/api/admin/articles/{id:int}` | Path `id` | `Article`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/articles` | JSON `ArticleRequest`: `{ title, summary?, content?, imageUrl? }` | `Article` tạo mới | **Chưa có** |
| PUT | `/api/admin/articles/{id:int}` | Path `id`; JSON cùng `ArticleRequest` | `Article` cập nhật | **Chưa có** |
| DELETE | `/api/admin/articles/{id:int}` | Path `id` | `data:null`; 404 nếu không có | **Chưa có** |

### Rạp

Controller `/api/admin/cinemas` — `AdminOnly`. `Cinema` data fields: `id`, `name`, `address?`, `phone?`, `status?`, `roomCount`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/cinemas` | — | `Cinema[]`: `id`, `name`, `address?`, `phone?`, `status?`, `roomCount` | **Chưa có** |
| GET | `/api/admin/cinemas/{id}` | Path `id` (string) | `Cinema`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/cinemas` | JSON `CinemaRequest`: `{ id?, name, address?, phone?, status? }` | `Cinema` sau upsert | **Chưa có** |
| POST | `/api/admin/cinemas/{id}/toggle` | Path `id` | `Cinema` hoặc `null`; không có item thì HTTP 200, `success:false` | **Chưa có** |

### Khách hàng quản trị

Controller `/api/admin/customers` — `AdminOnly`. `AdminCustomerResponse`: `id`, `fullName`, `username`, `email?`, `phoneNumber?`, `birthDate?`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/customers` | — | `AdminCustomerResponse[]` | **Chưa có** |
| GET | `/api/admin/customers/{id}` | Path `id` (string) | `AdminCustomerResponse`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/customers` | JSON `AdminCustomerUpsertRequest`: `{ fullName, username, password, email?, phoneNumber?, birthDate? }` | `AdminCustomerResponse`; validation/service error có thể trả HTTP 400 | **Chưa có** |
| PUT | `/api/admin/customers/{id}` | Path `id`; JSON cùng `AdminCustomerUpsertRequest` | `AdminCustomerResponse`; 404 nếu không có | **Chưa có** |
| DELETE | `/api/admin/customers/{id}` | Path `id` | `data:null`; 404 nếu không có | **Chưa có** |

### Dashboard

Controller `/api/admin/dashboard` — `AdminOnly`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/dashboard` | — | `AdminDashboardResponse`: `totalMovies`, `totalShowtimes`, `totalCustomers`, `totalAdmins`, `totalPaidRevenue` | **Chưa có** |

### Sự kiện

Controller `/api/admin/events` — `AdminOnly`. `Event` fields: `id`, `title`, `description?`, `imageUrl?`, `startDate?`, `endDate?`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/events` | — | `Event[]`: `id`, `title`, `description?`, `imageUrl?`, `startDate?`, `endDate?` | **Chưa có** |
| GET | `/api/admin/events/{id:int}` | Path `id` | `Event`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/events` | JSON `EventRequest`: `{ title, description?, imageUrl?, startDate?, endDate? }` | `Event` tạo mới | **Chưa có** |
| PUT | `/api/admin/events/{id:int}` | Path `id`; JSON cùng `EventRequest` | `Event` cập nhật | **Chưa có** |
| DELETE | `/api/admin/events/{id:int}` | Path `id` | `data:null`; 404 nếu không có | **Chưa có** |

### Thể loại

Controller `/api/admin/genres` — `AdminOnly`. `Genre` fields: `id`, `name`, `description?`, `createdAt`, `updatedAt?`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/genres` | — | `Genre[]`: `id`, `name`, `description?`, `createdAt`, `updatedAt?` | **Chưa có** |
| GET | `/api/admin/genres/{id:int}` | Path `id` | `Genre`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/genres` | JSON `GenreRequest`: `{ name, description? }` | `Genre` tạo mới | **Chưa có** |
| PUT | `/api/admin/genres/{id:int}` | Path `id`; JSON cùng `GenreRequest` | `Genre` cập nhật | **Chưa có** |
| DELETE | `/api/admin/genres/{id:int}` | Path `id` | `data:null`; 404 nếu không có | **Chưa có** |

### Điểm thưởng khách hàng

Controller `/api/admin/loyalty-points` — `AdminOnly`. `LoyaltyPoint` result fields: `id`, `customerId`, `customerName?`, `rewardPoints`, `earnedPoints`, `usedPoints`, computed `balance`, `pointsToMoney`, `moneyPerPoint`, `pointValue`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/loyalty-points` | Query `search?` | `LoyaltyPoint[]` với các trường nêu trên | **Chưa có** |
| GET | `/api/admin/loyalty-points/{customerId}` | Path `customerId` (string) | `LoyaltyPoint` với các trường nêu trên | **Chưa có** |
| POST | `/api/admin/loyalty-points/adjust` | JSON `LoyaltyAdjustRequest`: `{ customerId, points, action }` | `LoyaltyPoint` cập nhật. Chỉ `action:"subtract"` (không phân biệt hoa thường) trừ điểm; giá trị khác cộng điểm. `points` phải lớn hơn 0, và không thể trừ quá số dư. | **Chưa có** |

### Vai trò và quyền

Controller `/api/admin/roles` — `AdminOnly`. `AdminRoleResponse`: `id`, `name`, `isMaster`, `isActive`; `AdminPermissionResponse`: `id`, `name`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/roles` | — | `AdminRoleResponse[]` | **Chưa có** |
| GET | `/api/admin/roles/{id:int}` | Path `id` | `AdminRoleResponse`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/roles` | JSON `AdminRoleUpsertRequest`: `{ name, isMaster, isActive }` | `AdminRoleResponse` | **Chưa có** |
| PUT | `/api/admin/roles/{id:int}` | Path `id`; JSON cùng `AdminRoleUpsertRequest` | `AdminRoleResponse`; 404 nếu không có | **Chưa có** |
| DELETE | `/api/admin/roles/{id:int}` | Path `id` | `data:null`; 404 nếu không có | **Chưa có** |
| GET | `/api/admin/roles/permissions` | — | `AdminPermissionResponse[]` | **Chưa có** |
| GET | `/api/admin/roles/{roleId:int}/permissions` | Path `roleId` | `AdminPermissionResponse[]` | **Chưa có** |
| POST | `/api/admin/roles/permissions/assign` | JSON `AdminPermissionAssignRequest`: `{ roleId, permissionId }` | `data:null` | **Chưa có** |
| DELETE | `/api/admin/roles/{roleId:int}/permissions/{permissionId:int}` | Path `roleId`, `permissionId` | `data:null` | **Chưa có** |

### Phòng chiếu

Controller `/api/admin/rooms` — `AdminOnly`. `Room` fields: `id`, `name`, `cinemaId?`, `cinemaName?`, `seatCount`, `status?`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/rooms` | Query `cinemaId?` | `Room[]` (service entity) | **Chưa có** |
| GET | `/api/admin/rooms/{id}` | Path `id` (string) | `Room`; 404 nếu không có | **Chưa có** |
| POST | `/api/admin/rooms` | JSON `RoomRequest`: `{ id?, name, seatCount, status? }` | `Room` sau upsert; lỗi validation/service HTTP 400 | **Chưa có** |
| POST | `/api/admin/rooms/{id}/toggle` | Path `id` | `Room` cập nhật; 404 nếu không có | **Chưa có** |

### Ghế

Controller `/api/admin/seats` — `AdminOnly`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/seats` | Query `roomId?`, `search?` | `Seat[]`: `id`, `roomId`, `row`, `column`, `seatType?`, `isBooked`, `status`, `price` | **Chưa có** |
| GET | `/api/admin/seats/map/{roomId}` | Path `roomId` (string) | Sơ đồ ghế do service trả về (shape không được biểu diễn bằng DTO riêng trong controller) | **Chưa có** |
| POST | `/api/admin/seats/{id}/toggle` | Path `id` (string); query `roomId?` | Trạng thái từ service; 404 nếu không có | **Chưa có** |

## 1. Hóa đơn và check-in

Controller: `AdminInvoicesController`, route gốc `/api/admin/invoices`, policy `AdminOnly`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/invoices` | — | `InvoiceDetail[]`: `invoiceId`, `customerName?`, `movieTitle?`, `showDate?`, `startTime?`, `totalAmount`, `status?`, `paymentDate`, `note?`, `transactionRef?`, `isCheckedIn`, `checkedInAt?`, `lineItems[]`. `InvoiceLineItem`: `id`, `invoiceId`, `lineType`, `ticketId?`, `comboId?`, `itemName`, `quantity`, `unitPrice`, `isCheckedIn`, `checkedInAt?`. | **Chưa có** |
| GET | `/api/admin/invoices/{id}` | Path `id` (string) | `InvoiceDetail` như trên (service có thể trả `null` nếu không tìm thấy). | **Chưa có** |
| GET | `/api/admin/invoices/sync-report` | — | `InvoiceSyncReport`: `totalInvoices`, `issueCount`, `fixedTicketLines`, `fixedComboLines`, `normalizedPayments`, `normalizedInvoices`, `items[]`. Item: `invoiceId`, `transactionRef?`, `customerId?`, `status?`, `paymentDate`, `lineCount`, `ticketLineCount`, `comboLineCount`, `ticketCount`, `comboBookingCount`, `missingPayment`, `missingTicketLines`, `missingComboLines`, `missingMovieOrShowtime`, `duplicateTransactionRef`, `issues[]`, `hasIssue`. | **Chưa có** |
| POST | `/api/admin/invoices/sync` | — | `InvoiceSyncReport` như `GET /sync-report`; hành động đồng bộ có thể sửa dữ liệu hóa đơn/chi tiết liên quan. | **Chưa có** |
| POST | `/api/admin/invoices/check-in` | JSON `CheckInRequest`: `{ qrText }` | `CheckInResult`: `success`, `message`, `invoiceId?`, `transactionRef?`, `ticketId?`, `seatId?`, `customerName?`, `movieTitle?`, `showDate?`, `startTime?`, `totalAmount`, `status?`, `isCheckedIn`, `checkedInAt?`. Giá trị `success` trong envelope lấy từ result. | **Chưa có** |

## 2. Thống kê

Controller: `AdminStatisticsController`, route gốc `/api/admin/statistics`, policy `AdminOnly`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/statistics/revenue` | Query `year?` (integer) | `RevenueStatistics`: `totalRevenue`, `totalOrders`, `totalTickets`, `checkedInTickets`, `averageOrderValue`, `todayRevenue`, `currentMonthRevenue`, `currentYearRevenue`, `statisticsDate`, `monthlyData[]`, `paymentMethods[]`, `topMovies[]`, `dailyData[]`, `hourlyData[]`, `weekdayData[]`, `topCombos[]`, `topCustomers[]`. `RevenueDataPoint`: `label`, `value`; `PaymentMethodStat`: `method`, `orderCount`, `revenue`. | **Chưa có** |

## 3. Suất chiếu

Controller: `AdminShowtimesController`, route gốc `/api/admin/showtimes`, policy `AdminOnly`. Thành công trả `Envelope<T>`; response suất chiếu là `ShowtimeResponse`: `id`, `showDate`, `startTime`, `roomId`, `roomName`, `ticketPrice`, `totalSeats`, `availableSeats`, `status?`, `heldSeats`, `soldSeats`, `checkedInSeats`, `canEdit`, `canDelete`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/showtimes` | Query `roomId?`, `date?` (DateTime) | `ShowtimeResponse[]` | **Chưa có** |
| POST | `/api/admin/showtimes` | JSON `AdminShowtimeUpsertRequest`: `{ movieId, roomId, showDate, startTime, ticketPrice, status? }` (`status` được khai báo nhưng action tạo không dùng) | `ShowtimeResponse`; nếu không tạo được: HTTP 400, `success:false`, `data:null`. | **Chưa có** |
| PUT | `/api/admin/showtimes/{id}` | Path `id` (string); JSON cùng `AdminShowtimeUpsertRequest` | `ShowtimeResponse`; nếu không tìm thấy: HTTP 404, `data:null`. | **Chưa có** |
| DELETE | `/api/admin/showtimes/{id}` | Path `id` | `data:null`; không tìm thấy: HTTP 404. | **Chưa có** |
| POST | `/api/admin/showtimes/{id}/cancel` | Path `id` | `data:null`; không tìm thấy: HTTP 404. | **Chưa có** |
| POST | `/api/admin/showtimes/generate` | JSON `AdminShowtimeGenerateRequest`: `{ movieIds, roomIds, fromDate, toDate, startTimes, ticketPrice }` | `ShowtimeGenerateResult`: `created`, `skipped`, `messages[]`. | **Chưa có** |
| POST | `/api/admin/showtimes/expire-passed` | — | `{ expired }` (integer) | **Chưa có** |

## 4. Phim

Controller: `AdminMoviesController`, route gốc `/api/admin/movies`, policy `AdminOnly`. `MovieResponse` (GET) field JSON: `id`, `title`, `genre?`, `durationMinutes`, `director?`, `cast?`, `country?`, `ageRestriction?`, `description?`, `posterUrl?`, `trailerUrl?`, `releaseDate?`, `endDate?`, `status?`. Upsert request `AdminMovieUpsertRequest`: `{ title, genre?, durationMinutes, director?, cast?, country?, ageRestriction?, description?, posterUrl?, trailerUrl?, releaseDate?, endDate?, status? }`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/movies` | Query `search?` | `MovieResponse[]` | **Chưa có** |
| GET | `/api/admin/movies/{id}` | Path `id` (string) | `MovieResponse`; không tìm thấy: HTTP 404, `data:null`. | **Chưa có** |
| POST | `/api/admin/movies` | JSON `AdminMovieUpsertRequest` như trên | `Movie` entity với các trường JSON như `MovieResponse` (tạo thành công HTTP 200). | **Chưa có** |
| PUT | `/api/admin/movies/{id}` | Path `id`; JSON `AdminMovieUpsertRequest` | `Movie` entity với các trường JSON như `MovieResponse`; không tìm thấy: HTTP 404, `data:null`. | **Chưa có** |
| DELETE | `/api/admin/movies/{id}` | Path `id` | `data:null`; không tìm thấy: HTTP 404. | **Chưa có** |

## 5. Voucher

Controller: `AdminVouchersController`, route gốc `/api/admin/vouchers`, policy `AdminOnly`. `Voucher` entity fields: `id`, `customerId`, `customerName?`, `isGlobal`, `code`, `description?`, `expiredAt`, `discountValue`, `discountType`, `minOrderAmount`, `maxDiscountAmount`, `isUsed`, `usedAt?`, `usedTransactionRef?`, `isExpired`, `isActive`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/vouchers` | Query `search?` | `Voucher[]` | **Chưa có** |
| GET | `/api/admin/vouchers/{id}` | Path `id` (string) | `Voucher`; not found: HTTP 404, `data:null`. | **Chưa có** |
| POST | `/api/admin/vouchers` | JSON `VoucherRequest`: `{ id, customerId, isGlobal, code, description?, expiredAt, discountValue, discountType, minOrderAmount, maxDiscountAmount }`. When `isGlobal:true`, service is called with customer ID `ALL`. | `Voucher` sau upsert | **Chưa có** |
| PUT | `/api/admin/vouchers/{id}` | Path `id`; same JSON `VoucherRequest` (path `id` là id được upsert) | `Voucher` sau upsert | **Chưa có** |
| POST | `/api/admin/vouchers/validate` | JSON `VoucherValidateRequest`: `{ customerId, code, subtotal }` | `VoucherValidationResult`: `success`, `message`, `code?`, `discountAmount`, `finalAmount`. Envelope `success/message` lấy từ result. | **Chưa có** |
| DELETE | `/api/admin/vouchers/{id}` | Path `id` | `data:null`; not found: HTTP 404. | **Chưa có** |

## 6. Combo

Controller: `AdminCombosController`, route gốc `/api/admin/combos`, policy `AdminOnly`. `ComboRequest`: `{ id, name, price, description?, imageUrl? }`. `Combo` entity: `id`, `name`, `price`, `description?`, `imageUrl?`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/combos` | — | `Combo[]` | **Chưa có** |
| GET | `/api/admin/combos/{id}` | Path `id` (string) | `Combo`; not found: HTTP 404, `data:null`. | **Chưa có** |
| POST | `/api/admin/combos` | JSON `ComboRequest` | `Combo` sau upsert | **Chưa có** |
| PUT | `/api/admin/combos/{id}` | Path `id`; JSON `ComboRequest` | `Combo` sau upsert; path `id` được dùng thay cho `request.id`. | **Chưa có** |
| DELETE | `/api/admin/combos/{id}` | Path `id` | `data:null`; not found: HTTP 404. | **Chưa có** |

## 7. Nhân viên

Controller: `AdminEmployeesController`, route gốc `/api/admin/employees`, policy `AdminOnly`. Request `AdminEmployeeUpsertRequest`: `{ fullName, birthDate?, phoneNumber?, email?, startDate?, roleId?, isActive }`. Response `AdminEmployeeResponse`: `id`, `fullName`, `birthDate?`, `phoneNumber?`, `email?`, `startDate?`, `roleId?`, `roleName?`, `isActive`.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/employees` | — | `AdminEmployeeResponse[]` | **Chưa có** |
| GET | `/api/admin/employees/{id:int}` | Path `id` (integer) | `AdminEmployeeResponse`; not found: HTTP 404, `data:null`. | **Chưa có** |
| POST | `/api/admin/employees` | JSON `AdminEmployeeUpsertRequest` | `Employee` entity: `id`, `fullName`, `birthDate?`, `phoneNumber?`, `email?`, `startDate?`, `roleId?`, `roleName?`, `isActive` (same fields as `AdminEmployeeResponse`) | **Chưa có** |
| PUT | `/api/admin/employees/{id:int}` | Path `id`; JSON cùng `AdminEmployeeUpsertRequest` | `Employee` entity với các trường trên; not found: HTTP 404, `data:null`. | **Chưa có** |
| DELETE | `/api/admin/employees/{id:int}` | Path `id` (integer) | `data:null`; not found: HTTP 404. | **Chưa có** |

## 8. Hoàn tiền

Controller: `AdminRefundsController`, route gốc `/api/admin/refunds`, policy `AdminOnly`. `RefundRequest` entity: `id`, `invoiceId`, `ticketId?`, `customerId`, `customerName?`, `movieTitle?`, `showDate?`, `startTime?`, `reason`, `status`, `requestedAt`, `approvedAt?`, `rejectedAt?`, `refundAmount`, `adminNote?`. `RefundRequestResult`: `success`, `message`, `data?` (nested `RefundRequest`). Action body DTO `RefundActionRequest`: `{ adminNote? }`; body may be omitted/null.

| Method | Route | Request | Response `data` chính | Android API notes |
|---|---|---|---|---|
| GET | `/api/admin/refunds` | Query `status?` | `RefundRequest[]` | **Chưa có** |
| POST | `/api/admin/refunds/{id:int}/approve` | Path `id` (integer); optional JSON `RefundActionRequest` | `RefundRequestResult` (envelope `success/message` mirror nested result) | **Chưa có** |
| POST | `/api/admin/refunds/{id:int}/reject` | Path `id` (integer); optional JSON `RefundActionRequest` | `RefundRequestResult` (envelope `success/message` mirror nested result) | **Chưa có** |

## DTO/entity source map

- Common envelope and serializer setup: `CinemaBD.Api/Contracts/Common/ApiResponse.cs`, `CinemaBD.Api/Program.cs`.
- Authorization policy `AdminOnly`: `CinemaBD.Api/Program.cs`.
- Admin request/response contracts: `CinemaBD.Api/Contracts/Admin/` (including `AdminShowtimeUpsertRequest.cs`, `AdminMovieUpsertRequest.cs`, `AdminEmployeeUpsertRequest.cs`, `AdminEmployeeResponse.cs`, customer/dashboard/role/permission contracts); controller-local request records are in their respective files under `CinemaBD.Api/Controllers/Admin/`. `ComboRequest` is declared in `AdminGenresController.cs` and used by the combo controller.
- Returned domain fields: `CinemaBD.Domain/Entities/InvoiceDetail.cs`, `InvoiceSyncModels.cs`, `CheckInResult.cs`, `RevenueStatistics.cs`, `ShowtimeDetail.cs`, `Movie.cs`, `Voucher.cs`, `Combo.cs`, `Employee.cs`, `RefundModels.cs`, `Article.cs`, `Cinema.cs`, `Customer.cs`, `Event.cs`, `Genre.cs`, `LoyaltyPoint.cs`, `Room.cs`, and `Seat.cs`. Loyalty-point mapping is in `CinemaBD.Infrastructure/Services/AdminLoyaltyPointService.cs`.
