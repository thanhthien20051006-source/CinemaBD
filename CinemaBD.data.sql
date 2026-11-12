USE [CinemaBD]
GO
ALTER TABLE [dbo].[Admin] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[COMBODADAT] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[GHE] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HoaDonChiTiet] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HoaDons] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[LichSuHoaDons] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[NhanVien] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[PhanQuyen] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[SUATCHIEU] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[THANHTOAN] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[VE] NOCHECK CONSTRAINT ALL;
GO
SET IDENTITY_INSERT [dbo].[Admin] ON

INSERT [dbo].[Admin] ([AdminID], [Username], [Password], [FullName], [Email], [Phone], [Role], [CreatedAt], [IsActive], [MaCV]) VALUES (1, N'admin', N'202cb962ac59075b964b07152d234b70', N'Admin', N'thanhthien20051006@gmail.com', NULL, N'Admin', CAST(N'2026-05-26T18:01:10.993' AS DateTime), 1, 5)
SET IDENTITY_INSERT [dbo].[Admin] OFF
GO
SET IDENTITY_INSERT [dbo].[BaiViet] ON

INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (1, N'Top 5 bom tấn phòng vé năm 2025 không thể bỏ lỡ', N'Danh sách những bộ phim hành động, siêu anh hùng và hoạt hình nổi bật nhất năm 2025.', N'Fast & Furious X và Avatar 3 được dự đoán sẽ là hai bộ phim nổi bật, thu hút lượng lớn khán giả ra rạp trong thời gian tới.', N'top-bomtan-2025.jpg', CAST(N'2025-10-25T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (2, N'Review: Inside Out 2 – Khi cảm xúc trưởng thành cùng tuổi teen', N'Phần tiếp theo của Pixar khai thác tâm lý tuổi dậy thì đầy tinh tế.', N'Inside Out 2 khai thác hành trình Riley bước vào tuổi teen, khi các cảm xúc cũ buộc phải nhường chỗ cho những cảm xúc mới phức tạp hơn — từ lo âu, xấu hổ cho đến hoài nghi. Bộ phim cho thấy quá trình trưởng thành không phải là loại bỏ cảm xúc, mà là học cách sống chung với hỗn độn bên trong mình.', N'insideout2.jpg', CAST(N'2025-09-15T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (3, N'Marvel công bố Phase 6 – Sự trở lại của Iron Man?', N'Kevin Feige úp mở khả năng Tony Stark tái xuất trong đa vũ trụ.', N'Marvel công bố Phase 6 với hàng loạt dự án mới, nhưng thông tin Iron Man trở lại vẫn chỉ dừng ở mức đồn đoán. Studio không đưa ra xác nhận nào về việc Tony Stark xuất hiện lại, dù kỳ vọng của người hâm mộ vẫn rất lớn.', N'marvel-phase6.jpg', CAST(N'2025-08-21T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (4, N'Oppenheimer thắng lớn tại Oscar 2025', N'Bộ phim của Christopher Nolan giành 6 tượng vàng Oscar danh giá.', N'“Oppenheimer” tiếp tục khẳng định vị thế khi thắng lớn tại Oscar 2025. Bộ phim của Christopher Nolan được xướng tên ở nhiều hạng mục quan trọng như Phim hay nhất, Đạo diễn xuất sắc nhất và Nam chính xuất sắc nhất. Thành công này không chỉ ghi nhận chất lượng vượt trội của tác phẩm mà còn cho thấy sức hút mạnh mẽ của câu chuyện về cha đẻ bom nguyên tử. Đây được xem là một trong những chiến thắng ấn tượng nhất của lễ trao giải năm nay.', N'oppenheimer-oscar.jpg', CAST(N'2025-03-01T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (6, N'Câu chuyện hậu trường về Dune: Part Two', N'Denis Villeneuve tiết lộ những cảnh quay khổng lồ giữa sa mạc Jordan.', N'Dune: Part Two” đưa khán giả ra sau hậu trường của một trong những bom tấn hoành tráng nhất hiện nay. Bộ phim được thực hiện với quy mô lớn, nhiều cảnh quay ngoài sa mạc thật để mang lại cảm giác chân thực nhất. Đạo diễn Denis Villeneuve và ê-kíp dành nhiều tháng xây dựng bối cảnh, thiết kế trang phục và ghi hình trong điều kiện khắc nghiệt để tái hiện thế giới Arrakis một cách sống động. Hậu trường của phần hai cho thấy sự đầu tư tỉ mỉ và nỗ lực không ngừng của đoàn phim để tạo ra những khung hình ấn tượng mà khán giả thấy trên màn ảnh.', N'dune2.jpg', CAST(N'2025-05-12T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (7, N'Netflix ra mắt series kinh dị Việt Nam đầu tiên', N'Bộ phim ''Ngôi Làng Bên Kia Rừng'' mở đầu cho xu hướng mới của điện ảnh Việt.', N'Đây là bước đi táo bạo của Netflix tại thị trường Đông Nam Á...', N'netflix-vietnam.jpg', CAST(N'2025-06-02T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (8, N'Cinemax mở rạp chiếu hiện đại nhất Đông Nam Á tại TP.HCM', N'Rạp được trang bị màn hình IMAX Laser và ghế ngả tự động sang trọng.', N'Không chỉ là nơi xem phim, Cinemax còn là trung tâm giải trí công nghệ cao...', N'cinemax-hcm.jpg', CAST(N'2025-11-01T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (9, N'Phim Việt Nam bứt phá doanh thu quốc tế với The Horizon', N'Bộ phim hành động Việt lọt top 10 doanh thu châu Á tuần qua.', N'The Horizon đánh dấu bước tiến lớn cho điện ảnh Việt...', N'thehorizon.jpg', CAST(N'2025-10-12T00:00:00.000' AS DateTime))
INSERT [dbo].[BaiViet] ([MaBV], [TieuDe], [MoTa], [NoiDung], [Anh], [NgayDang]) VALUES (10, N'Trailer mới của Avatar 3 khiến fan phát sốt', N'Công nghệ motion-capture tiếp tục được đẩy lên tầm cao mới.', N'James Cameron tiếp tục khiến khán giả trầm trồ với kỹ xảo ấn tượng...', N'avatar3.jpg', CAST(N'2025-09-28T00:00:00.000' AS DateTime))
SET IDENTITY_INSERT [dbo].[BaiViet] OFF
GO
SET IDENTITY_INSERT [dbo].[ChucNang] ON

INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (1, N'Quản lý phim')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (2, N'Quản lý lịch chiếu')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (3, N'Quản lý phòng chiếu')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (4, N'Quản lý vé')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (5, N'Quản lý người dùng')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (6, N'Báo cáo thống kê')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (7, N'Quản lý tin tức')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (8, N'Quản lý liên hệ')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (9, N'Quản lý slider')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (10, N'Cấu hình hệ thống')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (11, N'Quản lý combo')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (12, N'Quản lý rạp')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (13, N'Quản lý nhân viên')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (14, N'Phân quyền')
SET IDENTITY_INSERT [dbo].[ChucNang] OFF
GO
SET IDENTITY_INSERT [dbo].[ChucVu] ON

INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (1, N'Nhân viên bán vé', 0, 1, CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (2, N'Nhân viên kỹ thuật', 0, 1, CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (3, N'Nhân viên vệ sinh', 0, 1, CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (4, N'Quản lý rạp', 0, 1, CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (5, N'Quản trị hệ thống', 1, 1, CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[ChucVu] OFF
GO
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB01', N'Combo 1 Bắp 1 Nước', CAST(65000 AS Decimal(18, 0)), N'1 Bắp ngọt lớn + 1 Nước ngọt lớn tuỳ chọn', N'/images/combo/combo1.png')
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB02', N'Combo 1 Bắp 2 Nước', CAST(85000 AS Decimal(18, 0)), N'1 Bắp ngọt lớn + 2 Nước ngọt lớn tuỳ chọn', N'/images/combo/combo2.png')
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB03', N'Combo Gia Đình', CAST(120000 AS Decimal(18, 0)), N'2 Bắp ngọt lớn + 3 Nước ngọt lớn tuỳ chọn', N'/images/combo/combo3.png')
GO
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH01', N'Nguyễn Văn A', N'0901234567', N'nguyenvana@gmail.com', N'nguyenvana', CAST(N'2000-01-01' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH02', N'Trần Thị B', N'0912345678', N'tranthib@gmail.com', N'tranthib', CAST(N'1995-05-15' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH03', N'Lê Văn C', N'0923456789', N'levanc@gmail.com', N'levanc', CAST(N'1998-09-20' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH04', N'Phạm Thị D', N'0934567890', N'phamthid@gmail.com', N'phamthid', CAST(N'2002-12-10' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH05', N'Hoàng Văn E', N'0945678901', N'hoangvane@gmail.com', N'hoangvane', CAST(N'1990-03-25' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
GO
SET IDENTITY_INSERT [dbo].[NhanVien] ON

INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (1, N'Nguyễn Văn An', CAST(N'1998-05-12' AS Date), N'0901234567', N'an.nguyen@cinemabd.vn', CAST(N'2023-01-15' AS Date), 1, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (2, N'Trần Thị Bích', CAST(N'2000-08-20' AS Date), N'0912345678', N'bich.tran@cinemabd.vn', CAST(N'2023-03-01' AS Date), 1, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (3, N'Lê Hoàng Cường', CAST(N'1995-11-03' AS Date), N'0923456789', N'cuong.le@cinemabd.vn', CAST(N'2022-06-10' AS Date), 2, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (4, N'Phạm Minh Dũng', CAST(N'1993-02-18' AS Date), N'0934567890', N'dung.pham@cinemabd.vn', CAST(N'2021-09-01' AS Date), 4, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (5, N'Võ Thị Em', CAST(N'1989-07-25' AS Date), N'0945678901', N'em.vo@cinemabd.vn', CAST(N'2020-04-15' AS Date), 5, 1)
SET IDENTITY_INSERT [dbo].[NhanVien] OFF
GO
SET IDENTITY_INSERT [dbo].[PhanQuyen] ON

INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (1, 5, 1)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (2, 5, 2)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (3, 5, 3)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (4, 5, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (5, 5, 5)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (6, 5, 6)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (7, 5, 7)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (8, 5, 8)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (9, 5, 9)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (10, 5, 10)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (11, 5, 11)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (12, 5, 12)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (13, 5, 13)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (14, 5, 14)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (15, 4, 1)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (16, 4, 2)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (17, 4, 3)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (18, 4, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (19, 4, 6)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (20, 1, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (21, 1, 11)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (22, 2, 3)
SET IDENTITY_INSERT [dbo].[PhanQuyen] OFF
GO
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'P01', N'Lật Mặt 7: Một Điều Ước', N'Gia đình, Tâm lý', 138, N'Lý Hải', N'Thanh Hiền, Trương Minh Cường, Đinh Y Nhung, Quách Ngọc Tuyên', N'Việt Nam', 13, N'Câu chuyện gia đình đầy cảm động xoay quanh người mẹ và những người con.', N'/images/movies/lat-mat-7.jpg', N'https://www.youtube.com/watch?v=latmat7', CAST(N'2024-04-26' AS Date), CAST(N'2024-06-30' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'P02', N'Deadpool & Wolverine', N'Hành động, Hài, Siêu anh hùng', 127, N'Shawn Levy', N'Ryan Reynolds, Hugh Jackman, Emma Corrin, Matthew Macfadyen', N'Mỹ', 18, N'Deadpool hợp tác cùng Wolverine trong một nhiệm vụ bảo vệ đa vũ trụ.', N'/images/movies/deadpool-wolverine.jpg', N'https://www.youtube.com/watch?v=deadpool3', CAST(N'2024-07-26' AS Date), CAST(N'2024-09-15' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'P03', N'Inside Out 2', N'Hoạt hình, Phiêu lưu, Hài', 96, N'Kelsey Mann', N'Amy Poehler, Maya Hawke, Kensington Tallman, Liza Lapira', N'Mỹ', 0, N'Riley bước vào tuổi dậy thì với những cảm xúc mới xuất hiện tại Tổng Hành Dinh.', N'/images/movies/inside-out-2.jpg', N'https://www.youtube.com/watch?v=insideout2', CAST(N'2024-06-14' AS Date), CAST(N'2024-08-31' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'P04', N'Ma Da', N'Kinh dị, Giật gân', 95, N'Nguyễn Hữu Hoàng', N'Việt Hương, NSƯT Thành Lộc, Cẩm Ly, Trung Dân', N'Việt Nam', 16, N'Truyền thuyết về Ma Da kéo người dưới sông được tái hiện rùng rợn tại vùng sông nước.', N'/images/movies/ma-da.jpg', N'https://www.youtube.com/watch?v=mada', CAST(N'2024-08-16' AS Date), CAST(N'2024-10-15' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'P05', N'Joker: Folie à Deux', N'Tâm lý, Tội phạm, Nhạc kịch', 138, N'Todd Phillips', N'Joaquin Phoenix, Lady Gaga, Zazie Beetz, Brendan Gleeson', N'Mỹ', 18, N'Arthur Fleck gặp gỡ tình yêu định mệnh Harley Quinn tại Arkham State Hospital.', N'/images/movies/joker-2.jpg', N'https://www.youtube.com/watch?v=joker2', CAST(N'2024-10-04' AS Date), CAST(N'2024-11-30' AS Date), N'Sắp chiếu')
GO
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai], [MaRap]) VALUES (N'PC01', N'Phòng Chiếu 1 (Standard)', 50, N'Hoạt động', N'RAP01')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai], [MaRap]) VALUES (N'PC02', N'Phòng Chiếu 2 (VIP)', 40, N'Hoạt động', N'RAP01')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai], [MaRap]) VALUES (N'PC03', N'Phòng Chiếu 3 (IMAX)', 60, N'Hoạt động', N'RAP01')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai], [MaRap]) VALUES (N'PC04', N'Phòng Chiếu 4 (3D)', 45, N'Bảo trì', N'RAP01')
GO
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A01', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A02', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A03', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A04', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A05', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B01', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B02', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B03', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B04', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B05', N'PC01', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C01', N'PC01', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C02', N'PC01', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C03', N'PC01', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C04', N'PC01', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C05', N'PC01', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A01', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A02', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A03', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A04', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B01', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B02', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B03', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B04', N'PC02', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A01', N'PC03', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A02', N'PC03', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A03', N'PC03', N'Thường', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B01', N'PC03', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B02', N'PC03', N'VIP', N'Trống')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B03', N'PC03', N'VIP', N'Trống')
GO
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC01', N'P01', N'PC01', CAST(N'2026-10-15' AS Date), TIMEFROMPARTS(9, 30, 0, 0, 0), CAST(85000 AS Decimal(18, 0)), N'Hoạt động')
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC02', N'P01', N'PC01', CAST(N'2026-10-15' AS Date), TIMEFROMPARTS(14, 0, 0, 0, 0), CAST(85000 AS Decimal(18, 0)), N'Hoạt động')
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC03', N'P02', N'PC02', CAST(N'2026-10-15' AS Date), TIMEFROMPARTS(10, 0, 0, 0, 0), CAST(110000 AS Decimal(18, 0)), N'Hoạt động')
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC04', N'P02', N'PC03', CAST(N'2026-10-15' AS Date), TIMEFROMPARTS(19, 30, 0, 0, 0), CAST(150000 AS Decimal(18, 0)), N'Hoạt động')
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC05', N'P03', N'PC01', CAST(N'2026-10-16' AS Date), TIMEFROMPARTS(8, 30, 0, 0, 0), CAST(75000 AS Decimal(18, 0)), N'Hoạt động')
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC06', N'P04', N'PC02', CAST(N'2026-10-16' AS Date), TIMEFROMPARTS(21, 0, 0, 0, 0), CAST(110000 AS Decimal(18, 0)), N'Hoạt động')
GO
SET IDENTITY_INSERT [dbo].[SuKien] ON

INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (1, N'Thứ 3 Vui Vẻ – Đồng Giá Vé 50K', N'Áp dụng cho tất cả các suất chiếu vào ngày thứ 3 hàng tuần.', N'happy-tuesday.jpg', CAST(N'2025-01-01' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (2, N'Ưu Đãi Học Sinh - Sinh Viên', N'Giảm 20% giá vé khi xuất trình thẻ học sinh/sinh viên hợp lệ.', N'hssv-discount.jpg', CAST(N'2025-01-01' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (3, N'Lễ Hội Phim Kinh Dị Halloween', N'Thưởng thức các bộ phim kinh dị đình đám với giá vé ưu đãi và nhận quà tặng đặc biệt.', N'halloween-fest.jpg', CAST(N'2025-10-20' AS Date), CAST(N'2025-10-31' AS Date))
SET IDENTITY_INSERT [dbo].[SuKien] OFF
GO
SET IDENTITY_INSERT [dbo].[TheLoai] ON

INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (1, N'Hành động', N'Phim có nhiều pha hành động nghẹt thở', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (2, N'Tâm lý', N'Phim tập trung khai thác nội tâm nhân vật', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (3, N'Kinh dị', N'Phim gây cảm giác rùng rợn, sợ hãi', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (4, N'Hoạt hình', N'Phim hoạt hình cho trẻ em và gia đình', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (5, N'Hài', N'Phim mang lại tiếng cười cho khán giả', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (6, N'Viễn tưởng', N'Phim khoa học viễn tưởng', CAST(N'2026-05-26T18:01:00.0000000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[TheLoai] OFF
GO
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'VE01', N'KH01', N'SC01', N'A01', CAST(85000 AS Decimal(18, 0)), N'Đã thanh toán', CAST(N'2024-10-10T10:15:00.000' AS DateTime), N'TXN20241010001', 0, NULL)
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'VE02', N'KH01', N'SC01', N'A02', CAST(85000 AS Decimal(18, 0)), N'Đã thanh toán', CAST(N'2024-10-10T10:15:00.000' AS DateTime), N'TXN20241010001', 0, NULL)
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'VE03', N'KH02', N'SC03', N'A01', CAST(110000 AS Decimal(18, 0)), N'Đã thanh toán', CAST(N'2024-10-11T14:30:00.000' AS DateTime), N'TXN20241011002', 1, CAST(N'2024-10-15T09:45:00.0000000' AS DateTime2))
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'VE04', N'KH03', N'SC04', N'B01', CAST(150000 AS Decimal(18, 0)), N'Đã thanh toán', CAST(N'2024-10-12T09:00:00.000' AS DateTime), N'TXN20241012003', 0, NULL)
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'VE05', N'KH04', N'SC02', N'B01', CAST(85000 AS Decimal(18, 0)), N'Chờ thanh toán', CAST(N'2024-10-12T16:45:00.000' AS DateTime), NULL, 0, NULL)
GO
SET IDENTITY_INSERT [dbo].[COMBODADAT] ON

INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (1, N'TXN20241010001', N'CB02', 1, CAST(85000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (2, N'TXN20241011002', N'CB01', 1, CAST(65000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (3, N'TXN20241012003', N'CB03', 1, CAST(120000 AS Decimal(18, 0)))
SET IDENTITY_INSERT [dbo].[COMBODADAT] OFF
GO
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'HD01', N'KH01', N'TXN20241010001', CAST(255000.00 AS Decimal(18, 2)), CAST(N'2024-10-10T10:20:00.000' AS DateTime), N'Đã thanh toán', N'2 vé + 1 Combo 2', 0, NULL)
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'HD02', N'KH02', N'TXN20241011002', CAST(175000.00 AS Decimal(18, 2)), CAST(N'2024-10-11T14:35:00.000' AS DateTime), N'Đã thanh toán', N'1 vé VIP + 1 Combo 1', 1, CAST(N'2024-10-15T09:45:00.0000000' AS DateTime2))
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu], [DaCheckIn], [ThoiGianCheckIn]) VALUES (N'HD03', N'KH03', N'TXN20241012003', CAST(270000.00 AS Decimal(18, 2)), CAST(N'2024-10-12T09:05:00.000' AS DateTime), N'Đã thanh toán', N'1 vé IMAX + 1 Combo Gia Đình', 0, NULL)
GO
SET IDENTITY_INSERT [dbo].[HoaDonChiTiet] ON

INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (1, N'HD01', N'Ve', N'VE01', NULL, N'Vé xem phim - Ghế A01', 1, CAST(85000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (2, N'HD01', N'Ve', N'VE02', NULL, N'Vé xem phim - Ghế A02', 1, CAST(85000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], NULL, N'CB02', N'Combo 1 Bắp 2 Nước', 1, CAST(85000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (3, N'HD02', N'Ve', N'VE03', NULL, N'Vé xem phim VIP - Ghế A01', 1, CAST(110000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], NULL, N'CB01', N'Combo 1 Bắp 1 Nước', 1, CAST(65000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (4, N'HD03', N'Ve', N'VE04', NULL, N'Vé xem phim IMAX - Ghế B01', 1, CAST(150000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], NULL, N'CB03', N'Combo Gia Đình', 1, CAST(120000.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[HoaDonChiTiet] OFF
GO
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT01', N'VE01', CAST(255000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2024-10-10T10:15:00.000' AS DateTime), N'VNPAY', N'TXN20241010001', N'14567890', N'NCB', N'ATM', CAST(N'2024-10-10T10:20:00.000' AS DateTime), N'00', N'00', N'hash_demo_01')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT02', N'VE03', CAST(175000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2024-10-11T14:30:00.000' AS DateTime), N'VNPAY', N'TXN20241011002', N'14567891', N'VISA', N'CREDIT', CAST(N'2024-10-11T14:35:00.000' AS DateTime), N'00', N'00', N'hash_demo_02')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT03', N'VE04', CAST(270000 AS Decimal(18, 0)), N'Tiền mặt', N'Thành công', CAST(N'2024-10-12T09:00:00.000' AS DateTime), N'CASH', N'TXN20241012003', NULL, NULL, NULL, CAST(N'2024-10-12T09:05:00.000' AS DateTime), NULL, NULL, NULL)
GO
SET IDENTITY_INSERT [dbo].[LichSuHoaDons] ON

INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (1, N'HD01', N'Chờ thanh toán', CAST(N'2024-10-10T10:15:00.000' AS DateTime), N'Khởi tạo đơn hàng')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (2, N'HD01', N'Đã thanh toán', CAST(N'2024-10-10T10:20:00.000' AS DateTime), N'Thanh toán VNPAY thành công')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (3, N'HD02', N'Chờ thanh toán', CAST(N'2024-10-11T14:30:00.000' AS DateTime), N'Khởi tạo đơn hàng')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (4, N'HD02', N'Đã thanh toán', CAST(N'2024-10-11T14:35:00.000' AS DateTime), N'Thanh toán VNPAY thành công')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (5, N'HD02', N'Đã check-in', CAST(N'2024-10-15T09:45:00.000' AS DateTime), N'Khách đã vào rạp')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (6, N'HD03', N'Đã thanh toán', CAST(N'2024-10-12T09:05:00.000' AS DateTime), N'Thanh toán tại quầy')
SET IDENTITY_INSERT [dbo].[LichSuHoaDons] OFF
GO
ALTER TABLE [dbo].[Admin] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[COMBODADAT] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[GHE] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HoaDonChiTiet] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HoaDons] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[LichSuHoaDons] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[NhanVien] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[PhanQuyen] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[SUATCHIEU] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[THANHTOAN] CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[VE] CHECK CONSTRAINT ALL;
GO
