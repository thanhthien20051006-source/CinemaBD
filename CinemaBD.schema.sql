USE master
go
IF EXISTS(select * from sys.databases where name='CinemaBD')
DROP DATABASE CinemaBD

Go
Create Database CinemaBD

go
use CinemaBD

go
/****** Object:  Table [dbo].[COMBO]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[COMBO](
	[MaCombo] [nvarchar](50) NOT NULL,
	[TenCombo] [nvarchar](100) NOT NULL,
	[Gia] [decimal](18, 0) NOT NULL,
	[MoTa] [nvarchar](255) NULL,
	[Anh] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED
(
	[MaCombo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[COMBODADAT]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[COMBODADAT](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GatewayTxnRef] [nvarchar](50) NOT NULL,
	[MaCombo] [nvarchar](50) NOT NULL,
	[SoLuong] [int] NOT NULL,
	[Gia] [decimal](18, 0) NOT NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GHE]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GHE](
	[MaGhe] [nvarchar](50) NOT NULL,
	[MaPhong] [nvarchar](50) NOT NULL,
	[LoaiGhe] [nvarchar](20) NULL,
	[TrangThai] [nvarchar](50) NULL,
 CONSTRAINT [PK_GHE] PRIMARY KEY CLUSTERED
(
	[MaPhong] ASC,
	[MaGhe] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HoaDons]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HoaDons](
	[MaHD] [nvarchar](50) NOT NULL,
	[MaKH] [nvarchar](50) NOT NULL,
	[GatewayTxnRef] [nvarchar](50) NOT NULL,
	[TongTien] [decimal](18, 2) NOT NULL,
	[NgayThanhToan] [datetime] NOT NULL,
	[TrangThai] [nvarchar](50) NOT NULL,
	[GhiChu] [nvarchar](255) NULL,
	[DaCheckIn] [bit] NULL,
	[ThoiGianCheckIn] [datetime2] NULL,
PRIMARY KEY CLUSTERED
(
	[MaHD] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SUATCHIEU]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SUATCHIEU](
	[MaSuatChieu] [nvarchar](50) NOT NULL,
	[MaPhim] [nvarchar](50) NOT NULL,
	[MaPhong] [nvarchar](50) NOT NULL,
	[NgayChieu] [date] NOT NULL,
	[GioBatDau] [time](0) NOT NULL,
	[GiaVe] [decimal](18, 0) NOT NULL,
	[TrangThai] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED
(
	[MaSuatChieu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VE]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VE](
	[MaVe] [nvarchar](50) NOT NULL,
	[MaKH] [nvarchar](50) NULL,
	[MaSuatChieu] [nvarchar](50) NOT NULL,
	[MaGhe] [nvarchar](50) NOT NULL,
	[GiaVe] [decimal](18, 0) NOT NULL,
	[TrangThai] [nvarchar](50) NOT NULL,
	[NgayDat] [datetime] NOT NULL,
	[GatewayTxnRef] [nvarchar](50) NULL,
	[DaCheckIn] [bit] NULL,
	[ThoiGianCheckIn] [datetime2] NULL,
PRIMARY KEY CLUSTERED
(
	[MaVe] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_ChiTietHoaDon]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[vw_ChiTietHoaDon]
AS
    -- DÒNG VÉ (GHẾ)
    SELECT
        hd.MaHD,
        hd.MaKH,
        hd.GatewayTxnRef,
        N'Ve'        AS LoaiDong,       -- để biết đây là vé
        v.MaVe       AS MaVe,
        v.MaSuatChieu,
        v.MaGhe,
        sc.MaPhong,
        g.LoaiGhe,
        1            AS SoLuong,
        v.GiaVe      AS DonGia,
        CAST(v.GiaVe AS decimal(18,2)) AS ThanhTien,
        g.MaGhe + N' (' + ISNULL(g.LoaiGhe, N'') + N')' AS TenDong
    FROM HoaDons hd
    JOIN VE v
        ON v.GatewayTxnRef = hd.GatewayTxnRef
    JOIN SUATCHIEU sc
        ON sc.MaSuatChieu = v.MaSuatChieu
    JOIN GHE g
        ON g.MaPhong = sc.MaPhong
       AND g.MaGhe   = v.MaGhe

    UNION ALL

    -- DÒNG COMBO
    SELECT
        hd.MaHD,
        hd.MaKH,
        hd.GatewayTxnRef,
        N'Combo'     AS LoaiDong,
        NULL         AS MaVe,
        NULL         AS MaSuatChieu,
        NULL         AS MaGhe,
        NULL         AS MaPhong,
        NULL         AS LoaiGhe,
        cdd.SoLuong  AS SoLuong,
        cdd.Gia      AS DonGia,
        CAST(cdd.SoLuong * cdd.Gia AS decimal(18,2)) AS ThanhTien,
        cb.TenCombo  AS TenDong
    FROM HoaDons hd
    JOIN COMBODADAT cdd
        ON cdd.GatewayTxnRef = hd.GatewayTxnRef
    JOIN COMBO cb
        ON cb.MaCombo = cdd.MaCombo;
GO
/****** Object:  Table [dbo].[Admin]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Admin](
	[AdminID] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](100) NOT NULL,
	[Password] [nvarchar](256) NOT NULL,
	[FullName] [nvarchar](100) NULL,
	[Email] [nvarchar](150) NULL,
	[Phone] [nvarchar](20) NULL,
	[Role] [nvarchar](50) NULL,
	[CreatedAt] [datetime] NULL,
	[IsActive] [bit] NULL,
	[MaCV] [int] NULL,
	[MustChangePassword] [bit] NOT NULL CONSTRAINT [DF_Admin_MustChangePassword] DEFAULT 0,
	[PasswordExpiresAt] [datetime2] NULL,
PRIMARY KEY CLUSTERED
(
	[AdminID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BaiViet]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BaiViet](
	[MaBV] [int] IDENTITY(1,1) NOT NULL,
	[TieuDe] [nvarchar](200) NULL,
	[MoTa] [nvarchar](500) NULL,
	[NoiDung] [nvarchar](max) NULL,
	[Anh] [nvarchar](500) NULL,
	[NgayDang] [datetime] NULL,
PRIMARY KEY CLUSTERED
(
	[MaBV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChucNang]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChucNang](
	[MaCN] [int] IDENTITY(1,1) NOT NULL,
	[TenChucNang] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED
(
	[MaCN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChucVu]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChucVu](
	[MaCV] [int] IDENTITY(1,1) NOT NULL,
	[TenChucVu] [nvarchar](100) NOT NULL,
	[IsMaster] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED
(
	[MaCV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DanhGia]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DanhGia](
	[MaDG] [int] IDENTITY(1,1) NOT NULL,
	[MaPhim] [nvarchar](50) NOT NULL,
	[MaKH] [nvarchar](50) NOT NULL,
	[NoiDung] [nvarchar](1000) NOT NULL,
	[NgayTao] [datetime] NOT NULL,
	[Rating] [int] NULL,
	[IsHidden] [bit] NULL,
PRIMARY KEY CLUSTERED
(
	[MaDG] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HoaDonChiTiet]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HoaDonChiTiet](
	[MaCT] [int] IDENTITY(1,1) NOT NULL,
	[MaHD] [nvarchar](50) NOT NULL,
	[LoaiDong] [nvarchar](20) NOT NULL,
	[MaVe] [nvarchar](50) NULL,
	[MaCombo] [nvarchar](50) NULL,
	[TenDong] [nvarchar](200) NOT NULL,
	[SoLuong] [int] NOT NULL,
	[DonGia] [decimal](18, 2) NOT NULL,
	[ThanhTien]  AS ([SoLuong]*[DonGia]) PERSISTED,
PRIMARY KEY CLUSTERED
(
	[MaCT] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[KHACHHANG]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KHACHHANG](
	[MaKH] [nvarchar](50) NOT NULL,
	[HoTen] [nvarchar](255) NOT NULL,
	[SDT] [nvarchar](20) NULL,
	[Email] [nvarchar](255) NULL,
	[TKhoan] [nvarchar](100) NOT NULL,
	[NgaySinh] [date] NULL,
	[MatKhau] [nvarchar](255) NULL,
	[TongChiTieu] [decimal](18, 0) NULL,
	[MustChangePassword] [bit] NOT NULL CONSTRAINT [DF_KHACHHANG_MustChangePassword] DEFAULT 0,
	[PasswordExpiresAt] [datetime2] NULL,
PRIMARY KEY CLUSTERED
(
	[MaKH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LichSuHoaDons]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LichSuHoaDons](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaHD] [nvarchar](50) NOT NULL,
	[TrangThai] [nvarchar](100) NOT NULL,
	[ThoiGian] [datetime] NOT NULL,
	[GhiChu] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NhanVien]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NhanVien](
	[MaNV] [int] IDENTITY(1,1) NOT NULL,
	[HoTen] [nvarchar](100) NOT NULL,
	[NgaySinh] [date] NULL,
	[SDT] [nvarchar](20) NULL,
	[Email] [nvarchar](100) NULL,
	[NgayBatDau] [date] NULL,
	[MaCV] [int] NULL,
	[TrangThai] [bit] NOT NULL,
 CONSTRAINT [PK_NhanVien] PRIMARY KEY CLUSTERED
(
	[MaNV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhanQuyen]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhanQuyen](
	[MaPQ] [int] IDENTITY(1,1) NOT NULL,
	[MaCV] [int] NULL,
	[MaCN] [int] NULL,
PRIMARY KEY CLUSTERED
(
	[MaPQ] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PHIM]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PHIM](
	[MaPhim] [nvarchar](50) NOT NULL,
	[TenPhim] [nvarchar](255) NOT NULL,
	[TheLoai] [nvarchar](100) NULL,
	[ThoiLuong] [int] NOT NULL,
	[DaoDien] [nvarchar](255) NULL,
	[DienVien] [nvarchar](500) NULL,
	[Nguon] [nvarchar](255) NULL,
	[GioiHanTuoi] [int] NULL,
	[MoTa] [nvarchar](max) NULL,
	[AnhDaiDien] [nvarchar](255) NULL,
	[Trailer] [nvarchar](255) NULL,
	[NgayKhoiChieu] [date] NULL,
	[NgayKetThuc] [date] NULL,
	[TrangThai] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED
(
	[MaPhim] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PHONGCHIEU]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PHONGCHIEU](
	[MaPhong] [nvarchar](50) NOT NULL,
	[TenPhong] [nvarchar](100) NOT NULL,
	[SoLuong] [int] NOT NULL,
	[TrangThai] [nvarchar](50) NULL,
	[MaRap] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED
(
	[MaPhong] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SuKien]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SuKien](
	[MaSK] [int] IDENTITY(1,1) NOT NULL,
	[TieuDe] [nvarchar](200) NOT NULL,
	[MoTa] [nvarchar](1000) NULL,
	[Anh] [nvarchar](500) NULL,
	[NgayBatDau] [date] NULL,
	[NgayKetThuc] [date] NULL,
PRIMARY KEY CLUSTERED
(
	[MaSK] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[THANHTOAN]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[THANHTOAN](
	[MaTT] [nvarchar](50) NOT NULL,
	[MaVe] [nvarchar](50) NULL,
	[SoTien] [decimal](18, 0) NOT NULL,
	[HinhThuc] [nvarchar](50) NOT NULL,
	[TrangThai] [nvarchar](50) NOT NULL,
	[NgayDat] [datetime] NOT NULL,
	[PaymentGateway] [nvarchar](50) NULL,
	[GatewayTxnRef] [nvarchar](50) NULL,
	[GatewayTransNo] [nvarchar](50) NULL,
	[BankCode] [nvarchar](50) NULL,
	[CardType] [nvarchar](50) NULL,
	[PayDate] [datetime] NULL,
	[VnpResponseCode] [nvarchar](10) NULL,
	[VnpTransactionStatus] [nvarchar](10) NULL,
	[SecureHash] [nvarchar](256) NULL,
PRIMARY KEY CLUSTERED
(
	[MaTT] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TheLoai]    Script Date: 10/12/2025 9:57:56 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TheLoai](
	[MaTL] [int] IDENTITY(1,1) NOT NULL,
	[TenTheLoai] [nvarchar](100) NOT NULL,
	[MoTa] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED
(
	[MaTL] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RevokedTokens] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'[dbo].[RevokedTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RevokedTokens]
    (
        [Jti] nvarchar(128) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RevokedTokens] PRIMARY KEY ([Jti])
    );
    CREATE INDEX [IX_RevokedTokens_ExpiresAtUtc] ON [dbo].[RevokedTokens] ([ExpiresAtUtc]);
END;
GO
/****** Object:  Table [dbo].[RAP] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'[dbo].[RAP]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RAP]
    (
        [MaRap] nvarchar(450) NOT NULL,
        [TenRap] nvarchar(max) NOT NULL,
        [DiaChi] nvarchar(max) NULL,
        [SoDienThoai] nvarchar(max) NULL,
        [TrangThai] nvarchar(max) NULL,
        CONSTRAINT [PK_RAP] PRIMARY KEY ([MaRap])
    );
END;
GO
/****** Object:  Table [dbo].[VOUCHER] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'[dbo].[VOUCHER]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VOUCHER]
    (
        [MaVoucher] nvarchar(450) NOT NULL,
        [MaKH] nvarchar(max) NOT NULL,
        [MaCode] nvarchar(max) NOT NULL,
        [MoTa] nvarchar(max) NULL,
        [NgayHetHan] datetime2 NOT NULL,
        [GiaTriGiam] decimal(18,2) NULL,
        [LoaiGiam] nvarchar(max) NULL,
        [DonToiThieu] decimal(18,2) NULL,
        [GiamToiDa] decimal(18,2) NULL,
        [DaSuDung] bit NULL,
        [NgaySuDung] datetime2 NULL,
        [GatewayTxnRef] nvarchar(max) NULL,
        CONSTRAINT [PK_VOUCHER] PRIMARY KEY ([MaVoucher])
    );
END;
GO
/****** Object:  Table [dbo].[TICHDIEM] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'[dbo].[TICHDIEM]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TICHDIEM]
    (
        [MaTichDiem] nvarchar(450) NOT NULL,
        [MaKH] nvarchar(max) NOT NULL,
        [DiemThuong] int NOT NULL CONSTRAINT [DF_TICHDIEM_DiemThuong] DEFAULT 0,
        [DiemCong] int NOT NULL CONSTRAINT [DF_TICHDIEM_DiemCong] DEFAULT 0,
        [DiemTru] int NOT NULL CONSTRAINT [DF_TICHDIEM_DiemTru] DEFAULT 0,
        CONSTRAINT [PK_TICHDIEM] PRIMARY KEY ([MaTichDiem])
    );
END;
GO
/****** Object:  Table [dbo].[RefundRequests] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'[dbo].[RefundRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefundRequests]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [MaHD] nvarchar(max) NOT NULL,
        [MaVe] nvarchar(max) NULL,
        [MaKH] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_RefundRequests_Status] DEFAULT N'Pending',
        [RequestedAt] datetime2 NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [RejectedAt] datetime2 NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [AdminNote] nvarchar(max) NULL,
        CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([Id])
    );
END;
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Admin__536C85E4E71BF05F]    Script Date: 10/12/2025 9:57:56 CH ******/
ALTER TABLE [dbo].[Admin] ADD UNIQUE NONCLUSTERED
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__TheLoai__327F958F2FE1D1FF]    Script Date: 10/12/2025 9:57:56 CH ******/
ALTER TABLE [dbo].[TheLoai] ADD UNIQUE NONCLUSTERED
(
	[TenTheLoai] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Admin] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Admin] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[BaiViet] ADD  DEFAULT (getdate()) FOR [NgayDang]
GO
ALTER TABLE [dbo].[ChucVu] ADD  CONSTRAINT [DF_ChucVu_IsMaster]  DEFAULT ((0)) FOR [IsMaster]
GO
ALTER TABLE [dbo].[ChucVu] ADD  CONSTRAINT [DF_ChucVu_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[ChucVu] ADD  CONSTRAINT [DF_ChucVu_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[DanhGia] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[NhanVien] ADD  CONSTRAINT [DF_NhanVien_TrangThai]  DEFAULT ((1)) FOR [TrangThai]
GO
ALTER TABLE [dbo].[PHIM] ADD  DEFAULT ((0)) FOR [ThoiLuong]
GO
ALTER TABLE [dbo].[PHONGCHIEU] ADD  DEFAULT ((0)) FOR [SoLuong]
GO
ALTER TABLE [dbo].[THANHTOAN] ADD  DEFAULT (getdate()) FOR [NgayDat]
GO
ALTER TABLE [dbo].[TheLoai] ADD  CONSTRAINT [DF_TheLoai_Created]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[VE] ADD  DEFAULT (getdate()) FOR [NgayDat]
GO
ALTER TABLE [dbo].[Admin]  WITH CHECK ADD  CONSTRAINT [FK_Admin_ChucVu] FOREIGN KEY([MaCV])
REFERENCES [dbo].[ChucVu] ([MaCV])
GO
ALTER TABLE [dbo].[Admin] CHECK CONSTRAINT [FK_Admin_ChucVu]
GO
ALTER TABLE [dbo].[COMBODADAT]  WITH CHECK ADD  CONSTRAINT [FK_COMBODADAT_COMBO] FOREIGN KEY([MaCombo])
REFERENCES [dbo].[COMBO] ([MaCombo])
GO
ALTER TABLE [dbo].[COMBODADAT] CHECK CONSTRAINT [FK_COMBODADAT_COMBO]
GO
ALTER TABLE [dbo].[GHE]  WITH CHECK ADD  CONSTRAINT [FK_GHE_PHONGCHIEU] FOREIGN KEY([MaPhong])
REFERENCES [dbo].[PHONGCHIEU] ([MaPhong])
GO
ALTER TABLE [dbo].[GHE] CHECK CONSTRAINT [FK_GHE_PHONGCHIEU]
GO
ALTER TABLE [dbo].[HoaDonChiTiet]  WITH CHECK ADD  CONSTRAINT [FK_HoaDonChiTiet_COMBO] FOREIGN KEY([MaCombo])
REFERENCES [dbo].[COMBO] ([MaCombo])
GO
ALTER TABLE [dbo].[HoaDonChiTiet] CHECK CONSTRAINT [FK_HoaDonChiTiet_COMBO]
GO
ALTER TABLE [dbo].[HoaDonChiTiet]  WITH CHECK ADD  CONSTRAINT [FK_HoaDonChiTiet_HoaDons] FOREIGN KEY([MaHD])
REFERENCES [dbo].[HoaDons] ([MaHD])
GO
ALTER TABLE [dbo].[HoaDonChiTiet] CHECK CONSTRAINT [FK_HoaDonChiTiet_HoaDons]
GO
ALTER TABLE [dbo].[HoaDonChiTiet]  WITH CHECK ADD  CONSTRAINT [FK_HoaDonChiTiet_VE] FOREIGN KEY([MaVe])
REFERENCES [dbo].[VE] ([MaVe])
GO
ALTER TABLE [dbo].[HoaDonChiTiet] CHECK CONSTRAINT [FK_HoaDonChiTiet_VE]
GO
ALTER TABLE [dbo].[HoaDons]  WITH CHECK ADD  CONSTRAINT [FK_HoaDon_KhachHang] FOREIGN KEY([MaKH])
REFERENCES [dbo].[KHACHHANG] ([MaKH])
GO
ALTER TABLE [dbo].[HoaDons] CHECK CONSTRAINT [FK_HoaDon_KhachHang]
GO
ALTER TABLE [dbo].[LichSuHoaDons]  WITH CHECK ADD  CONSTRAINT [FK_LichSuHoaDon_HoaDon] FOREIGN KEY([MaHD])
REFERENCES [dbo].[HoaDons] ([MaHD])
GO
ALTER TABLE [dbo].[LichSuHoaDons] CHECK CONSTRAINT [FK_LichSuHoaDon_HoaDon]
GO
ALTER TABLE [dbo].[NhanVien]  WITH CHECK ADD  CONSTRAINT [FK_NhanVien_ChucVu] FOREIGN KEY([MaCV])
REFERENCES [dbo].[ChucVu] ([MaCV])
GO
ALTER TABLE [dbo].[NhanVien] CHECK CONSTRAINT [FK_NhanVien_ChucVu]
GO
ALTER TABLE [dbo].[PhanQuyen]  WITH CHECK ADD FOREIGN KEY([MaCN])
REFERENCES [dbo].[ChucNang] ([MaCN])
GO
ALTER TABLE [dbo].[PhanQuyen]  WITH CHECK ADD FOREIGN KEY([MaCV])
REFERENCES [dbo].[ChucVu] ([MaCV])
GO
ALTER TABLE [dbo].[SUATCHIEU]  WITH CHECK ADD  CONSTRAINT [FK_SC_PHIM] FOREIGN KEY([MaPhim])
REFERENCES [dbo].[PHIM] ([MaPhim])
GO
ALTER TABLE [dbo].[SUATCHIEU] CHECK CONSTRAINT [FK_SC_PHIM]
GO
ALTER TABLE [dbo].[SUATCHIEU]  WITH CHECK ADD  CONSTRAINT [FK_SC_PHONG] FOREIGN KEY([MaPhong])
REFERENCES [dbo].[PHONGCHIEU] ([MaPhong])
GO
ALTER TABLE [dbo].[SUATCHIEU] CHECK CONSTRAINT [FK_SC_PHONG]
GO
ALTER TABLE [dbo].[THANHTOAN]  WITH CHECK ADD  CONSTRAINT [FK_TT_VE] FOREIGN KEY([MaVe])
REFERENCES [dbo].[VE] ([MaVe])
GO
ALTER TABLE [dbo].[THANHTOAN] CHECK CONSTRAINT [FK_TT_VE]
GO
ALTER TABLE [dbo].[VE]  WITH CHECK ADD  CONSTRAINT [FK_VE_KH] FOREIGN KEY([MaKH])
REFERENCES [dbo].[KHACHHANG] ([MaKH])
GO
ALTER TABLE [dbo].[VE] CHECK CONSTRAINT [FK_VE_KH]
GO
ALTER TABLE [dbo].[VE]  WITH CHECK ADD  CONSTRAINT [FK_VE_SC] FOREIGN KEY([MaSuatChieu])
REFERENCES [dbo].[SUATCHIEU] ([MaSuatChieu])
GO
ALTER TABLE [dbo].[VE] CHECK CONSTRAINT [FK_VE_SC]
GO
