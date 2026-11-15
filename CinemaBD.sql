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
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (6, N'Quản lý khuyến mãi')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (7, N'Báo cáo doanh thu')
INSERT [dbo].[ChucNang] ([MaCN], [TenChucNang]) VALUES (8, N'Phân quyền hệ thống')
SET IDENTITY_INSERT [dbo].[ChucNang] OFF
GO
SET IDENTITY_INSERT [dbo].[ChucVu] ON 

INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (1, N'Quản lý', 1, 1, CAST(N'2026-05-26T15:02:51.7265485' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (2, N'Nhân viên bán vé', 0, 1, CAST(N'2026-05-26T15:02:51.7265485' AS DateTime2), CAST(N'2026-05-26T15:32:31.1059172' AS DateTime2))
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (3, N'Nhân viên kiểm soát vé', 0, 1, CAST(N'2026-05-26T15:02:51.7265485' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (4, N'Kế toán', 0, 1, CAST(N'2026-05-26T15:02:51.7265485' AS DateTime2), NULL)
INSERT [dbo].[ChucVu] ([MaCV], [TenChucVu], [IsMaster], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (5, N'Quản trị viên hệ thống', 1, 1, CAST(N'2026-05-26T15:02:51.7265485' AS DateTime2), CAST(N'2025-12-05T13:56:21.0294360' AS DateTime2))
SET IDENTITY_INSERT [dbo].[ChucVu] OFF
GO
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB01', N'Bắp rang bơ + Trà tắc', CAST(55000 AS Decimal(18, 0)), N'1 bắp rang bơ + 1 ly trà tắc', N'combo1.jpg')
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB02', N'Bắp phomai + Sữa bắp', CAST(70000 AS Decimal(18, 0)), N'1 bắp phomai + 1 sữa bắp', N'combo2.jpg')
INSERT [dbo].[COMBO] ([MaCombo], [TenCombo], [Gia], [MoTa], [Anh]) VALUES (N'CB03', N'Combo đôi: 2 nước mía + bắp', CAST(95000 AS Decimal(18, 0)), N'1 bắp size L + 2 nước mía', N'combo3.jpg')
GO
SET IDENTITY_INSERT [dbo].[COMBODADAT] ON 

INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (1, N'TXN251117135929352', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (2, N'TXN251117142252853', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (3, N'TXN251117145252955', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (4, N'TXN251117151806575', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (5, N'TXN251117155327926', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (6, N'TXN251117155945271', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (7, N'TXN251117160359247', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (8, N'TXN251117161050790', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (9, N'TXN251117161821692', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (10, N'TXN251118004324102', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (11, N'TXN251118015550345', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (12, N'TXN251118022213515', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (13, N'TXN251118104902329', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (14, N'TXN251118125315595', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (15, N'TXN251118133126825', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (16, N'TXN251118133127570', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (17, N'TXN251118140003385', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (18, N'TXN251119012530275', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (19, N'TXN251120134440438', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (20, N'TXN251120140048776', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (21, N'TXN251120140248317', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (22, N'TXN251120141256937', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (23, N'TXN251120141706430', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (24, N'TXN251120142125668', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (25, N'TXN251120142618879', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (26, N'TXN251205135007031', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (27, N'TXN251205135007384', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (28, N'TXN251205152621712', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (29, N'TXN251208160123168', N'CB03', 1, CAST(95000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (30, N'TXN251209022622706', N'CB02', 1, CAST(70000 AS Decimal(18, 0)))
INSERT [dbo].[COMBODADAT] ([Id], [GatewayTxnRef], [MaCombo], [SoLuong], [Gia]) VALUES (31, N'TXN251210133939866', N'CB02', 3, CAST(210000 AS Decimal(18, 0)))
SET IDENTITY_INSERT [dbo].[COMBODADAT] OFF
GO
SET IDENTITY_INSERT [dbo].[DanhGia] ON 

INSERT [dbo].[DanhGia] ([MaDG], [MaPhim], [MaKH], [NoiDung], [NgayTao]) VALUES (1, N'VN001', N'KH1679797060', N'Phim xem ổn, nhịp khá tốt, kỹ xảo vừa đủ, dễ xem cuối tuần với bạn bè.', CAST(N'2025-11-20T17:50:22.120' AS DateTime))
INSERT [dbo].[DanhGia] ([MaDG], [MaPhim], [MaKH], [NoiDung], [NgayTao]) VALUES (2, N'VN002', N'KH1679797060', N'Nội dung không quá mới nhưng diễn viên diễn ổn, xem giải trí thì được.', CAST(N'2025-11-18T17:50:22.120' AS DateTime))
SET IDENTITY_INSERT [dbo].[DanhGia] OFF
GO
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A1', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A2', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A3', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A4', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A5', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A6', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B1', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B2', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B3', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B4', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B5', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B6', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C1', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C2', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C3', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C4', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C5', N'PC01', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C6', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D1', N'PC01', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D2', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D3', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D4', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D5', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D6', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E1', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E2', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E3', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E4', N'PC01', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E5', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E6', N'PC01', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A1', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A2', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A3', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A4', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A5', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A6', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B1', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B2', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B3', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B4', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B5', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B6', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C1', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C2', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C3', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C4', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C5', N'PC02', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C6', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D1', N'PC02', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D2', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D3', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D4', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D5', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D6', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E1', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E2', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E3', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E4', N'PC02', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E5', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E6', N'PC02', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A1', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A2', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A3', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A4', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A5', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A6', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B1', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B2', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B3', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B4', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B5', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B6', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C1', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C2', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C3', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C4', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C5', N'PC03', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C6', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D1', N'PC03', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D2', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D3', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D4', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D5', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D6', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E1', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E2', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E3', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E4', N'PC03', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E5', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E6', N'PC03', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A1', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A2', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A3', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A4', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A5', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A6', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B1', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B2', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B3', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B4', N'PC04', N'VIP', N'Hoạt động')
GO
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B5', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B6', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C1', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C2', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C3', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C4', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C5', N'PC04', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C6', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D1', N'PC04', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D2', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D3', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D4', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D5', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D6', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E1', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E2', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E3', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E4', N'PC04', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E5', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E6', N'PC04', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A1', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A2', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A3', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A4', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A5', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'A6', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B1', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B2', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B3', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B4', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B5', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'B6', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C1', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C2', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C3', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C4', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C5', N'PC05', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'C6', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D1', N'PC05', N'THUONG', N'Bảo trì')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D2', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D3', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D4', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D5', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'D6', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E1', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E2', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E3', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E4', N'PC05', N'VIP', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E5', N'PC05', N'THUONG', N'Hoạt động')
INSERT [dbo].[GHE] ([MaGhe], [MaPhong], [LoaiGhe], [TrangThai]) VALUES (N'E6', N'PC05', N'THUONG', N'Hoạt động')
GO
SET IDENTITY_INSERT [dbo].[HoaDonChiTiet] ON 

INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (1, N'HD639005704039164293', N'Combo', NULL, N'CB02', N'Bắp phomai + Sữa bắp', 1, CAST(70000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (2, N'HD639008693486456865', N'Combo', NULL, N'CB02', N'Bắp phomai + Sữa bắp', 1, CAST(70000.00 AS Decimal(18, 2)))
INSERT [dbo].[HoaDonChiTiet] ([MaCT], [MaHD], [LoaiDong], [MaVe], [MaCombo], [TenDong], [SoLuong], [DonGia]) VALUES (3, N'HD639009960165225521', N'Combo', NULL, N'CB02', N'Bắp phomai + Sữa bắp', 3, CAST(70000.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[HoaDonChiTiet] OFF
GO
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638990545576640283', N'KH1679797060', N'TXN251118022213515', CAST(120000.00 AS Decimal(18, 2)), CAST(N'2025-11-18T09:22:34.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15267730 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638990849913105660', N'KH1679797060', N'TXN251118104902329', CAST(145000.00 AS Decimal(18, 2)), CAST(N'2025-11-18T17:49:47.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15269074 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638990924208266670', N'KH1679797060', N'TXN251118125315595', CAST(145000.00 AS Decimal(18, 2)), CAST(N'2025-11-18T19:53:37.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15269287 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638990947152655377', N'KH20251118203033366', N'TXN251118133127570', CAST(120000.00 AS Decimal(18, 2)), CAST(N'2025-11-18T20:31:52.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15269327 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638991375711662465', N'KH1679797060', N'TXN251119012530275', CAST(120000.00 AS Decimal(18, 2)), CAST(N'2025-11-19T08:26:01.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15269861 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992683138160938', N'KH1679797060', N'TXN251120134440438', CAST(150000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T20:45:05.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273373 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992692888074719', N'KH1679797060', N'TXN251120140048776', CAST(175000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:01:21.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273407 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992694416594099', N'KH1679797060', N'TXN251120140248317', CAST(145000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:03:37.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273415 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992700406976778', N'KH1679797060', N'TXN251120141256937', CAST(230000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:13:50.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273442 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992702699918950', N'KH1679797060', N'TXN251120141706430', CAST(150000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:17:42.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273452 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992705720250561', N'KH1679797060', N'TXN251120142125668', CAST(225000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:22:28.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273459 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD638992708822768069', N'KH1679797060', N'TXN251120142618879', CAST(255000.00 AS Decimal(18, 2)), CAST(N'2025-11-20T21:27:38.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15273470 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD639005646452591974', N'KH20251118203033366', N'TXN251205135007384', CAST(150000.00 AS Decimal(18, 2)), CAST(N'2025-12-05T20:50:42.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15322554 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD639005704039164293', N'KH20251118203033366', N'TXN251205152621712', CAST(120000.00 AS Decimal(18, 2)), CAST(N'2025-12-05T22:26:41.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15322714 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD639008693486456865', N'KH20251118203033366', N'TXN251209022622706', CAST(150000.00 AS Decimal(18, 2)), CAST(N'2025-12-09T09:29:03.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15328854 - NH: NCB')
INSERT [dbo].[HoaDons] ([MaHD], [MaKH], [GatewayTxnRef], [TongTien], [NgayThanhToan], [TrangThai], [GhiChu]) VALUES (N'HD639009960165225521', N'KH20251118203033366', N'TXN251210133939866', CAST(260000.00 AS Decimal(18, 2)), CAST(N'2026-06-10T20:40:08.000' AS DateTime), N'Đã thanh toán', N'Thanh toán VNPAY - GD: 15333186 - NH: NCB')
GO
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH001', N'Trần Thanh Thiện', N'0901234569', N'thanhthien10062005@gmail.com', N'ttt', CAST(N'2005-06-10' AS Date), N'5fa285e1bebe0a6623e33afc04a1fbd5', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH1679797060', N'Trần Thanh Thiện', N'0901234569', N'thanhthien20051006@gmail.com', N'ttt123', CAST(N'2006-07-10' AS Date), N'202cb962ac59075b964b07152d234b70', CAST(0 AS Decimal(18, 0)))
INSERT [dbo].[KHACHHANG] ([MaKH], [HoTen], [SDT], [Email], [TKhoan], [NgaySinh], [MatKhau], [TongChiTieu]) VALUES (N'KH20251118203033366', N'Trần Thanh Thiện', N'0901234569', N'thanhthien20051006@gmail.com', N'ttt1', CAST(N'2006-06-10' AS Date), N'e10adc3949ba59abbe56e057f20f883e', NULL)
GO
SET IDENTITY_INSERT [dbo].[LichSuHoaDons] ON 

INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (1, N'HD638990545576640283', N'Đã thanh toán', CAST(N'2025-11-18T09:22:37.667' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (2, N'HD638990849913105660', N'Đã thanh toán', CAST(N'2025-11-18T17:49:51.310' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (3, N'HD638990924208266670', N'Đã thanh toán', CAST(N'2025-11-18T19:53:40.830' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (4, N'HD638990947152655377', N'Đã thanh toán', CAST(N'2025-11-18T20:31:55.267' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (5, N'HD638991375711662465', N'Đã thanh toán', CAST(N'2025-11-19T08:26:11.167' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (6, N'HD638992683138160938', N'Đã thanh toán', CAST(N'2025-11-20T20:45:13.817' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (7, N'HD638992692888074719', N'Đã thanh toán', CAST(N'2025-11-20T21:01:28.810' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (8, N'HD638992694416594099', N'Đã thanh toán', CAST(N'2025-11-20T21:04:01.660' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (9, N'HD638992700406976778', N'Đã thanh toán', CAST(N'2025-11-20T21:14:00.700' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (10, N'HD638992702699918950', N'Đã thanh toán', CAST(N'2025-11-20T21:17:49.993' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (11, N'HD638992705720250561', N'Đã thanh toán', CAST(N'2025-11-20T21:22:52.027' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (12, N'HD638992708822768069', N'Đã thanh toán', CAST(N'2025-11-20T21:28:02.280' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (13, N'HD639005646452591974', N'Đã thanh toán', CAST(N'2025-12-05T20:50:45.260' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (14, N'HD639005704039164293', N'Đã thanh toán', CAST(N'2025-12-05T22:26:43.967' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (16, N'HD639008693486456865', N'Đã thanh toán', CAST(N'2025-12-09T09:29:08.710' AS DateTime), N'VNPAY OK - Resp=00')
INSERT [dbo].[LichSuHoaDons] ([Id], [MaHD], [TrangThai], [ThoiGian], [GhiChu]) VALUES (17, N'HD639009960165225521', N'Đã thanh toán', CAST(N'2026-06-10T20:40:16.573' AS DateTime), N'VNPAY OK - Resp=00')
SET IDENTITY_INSERT [dbo].[LichSuHoaDons] OFF
GO
SET IDENTITY_INSERT [dbo].[NhanVien] ON 

INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (1, N'Admin', CAST(N'2005-06-10' AS Date), N'0353214873', N'thanhthien20051006@gmail.com', CAST(N'2024-01-01' AS Date), 1, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (2, N'Trần Văn Duyệt', CAST(N'1990-01-01' AS Date), N'0123456789', N'duyetan03@gmail.com', CAST(N'2024-01-01' AS Date), 2, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (3, N'Đỗ Văn Đại', CAST(N'1990-09-10' AS Date), N'0901234569', N'abcxyz@gmail.com', CAST(N'2024-01-01' AS Date), 3, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (4, N'Hồ Minh Tùng', CAST(N'1990-01-01' AS Date), N'0935303721', N'tungho.ntt@gmail.com', CAST(N'2024-01-01' AS Date), 4, 1)
INSERT [dbo].[NhanVien] ([MaNV], [HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (5, N'Nguyễn Quang Hùng', CAST(N'1990-01-01' AS Date), N'0935033321', N'hung.ntt@gmail.com', CAST(N'2024-01-01' AS Date), 5, 1)
SET IDENTITY_INSERT [dbo].[NhanVien] OFF
GO
SET IDENTITY_INSERT [dbo].[PhanQuyen] ON 

INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (1, 1, 1)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (2, 1, 2)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (3, 1, 3)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (4, 1, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (5, 1, 5)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (6, 1, 6)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (7, 1, 7)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (8, 1, 8)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (9, 2, 1)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (10, 2, 2)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (12, 3, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (13, 4, 7)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (14, 5, 1)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (15, 5, 2)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (16, 5, 3)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (17, 5, 4)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (18, 5, 5)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (19, 5, 6)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (20, 5, 7)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (21, 5, 8)
INSERT [dbo].[PhanQuyen] ([MaPQ], [MaCV], [MaCN]) VALUES (22, 2, 3)
SET IDENTITY_INSERT [dbo].[PhanQuyen] OFF
GO
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN001', N'Đảo Của Dân Ngụ Cư', N'Tâm lý', 95, N'Hồng Ánh', N'Phạm Hồng Phước, Ngọc Thanh Tâm', NULL, 18, N'Một lát cắt về thân phận con người nơi quán ăn trên đảo.', N'daocuadanngucu.jpg', N'https://youtu.be/0CiB8YTGkrw?si=atsR2cG6KkxZLlyr', CAST(N'2026-05-24' AS Date), CAST(N'2026-06-25' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN002', N'Lật Mặt: Hồi Ức', N'Hành động', 115, N'Lý Hải', N'Lý Hải, Thuý Diễm', NULL, 13, N'Hành trình lật mở sự thật qua những mảnh kí ức.', N'latmat.jpg', N'https://youtu.be/QdtPQ0wV53M?si=9bBONH4n--ju1LX_', CAST(N'2026-05-24' AS Date), CAST(N'2025-12-31' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN003', N'Nhà Bà Nữ 2', N'Hài, Gia đình', 110, N'Trấn Thành', N'Uyển Ân, Lê Giang', NULL, 13, N'Những va chạm đời sống gia đình Sài Gòn đương đại.', N'nhabanu.jpg', N'https://youtu.be/IkaP0KJWTsQ?si=5f-dLEppU4vyW_JU', CAST(N'2026-05-24' AS Date), CAST(N'2026-06-28' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN016', N'Tử Chiến Trên Không', N'Hành động, Quân sự', 128, N'Nguyễn Quang Dũng', N'Trọng Hiếu, Kaity Nguyễn', N'Việt Nam', 16, N'Sau một vụ tấn công bất ngờ vào không phận Việt Nam, đội phi công chiến đấu tinh nhuệ phải lao vào chiến dịch ngăn chặn kẻ địch. 
Bên dưới bầu trời rực lửa là những cuộc rượt đuổi sinh tử, xung đột nội tâm, và sự lựa chọn khó khăn giữa nhiệm vụ và gia đình.', N'tuchientrenkhong.jpg', N'https://youtube.com/abc016', CAST(N'2025-11-18' AS Date), CAST(N'2026-06-22' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN017', N'Dune: Hành Tinh Cát', N'Viễn tưởng, Hành động', 166, N'Denis V.', N'Timothée Chalamet, Zendaya', NULL, 13, N'Cuộc chiến giành quyền lực trên hành tinh sa mạc khắc nghiệt.', N'dune.jpg', N'https://youtube.com/trailer_dune2', CAST(N'2026-05-24' AS Date), CAST(N'2025-12-30' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN018', N'Inside Out: Cảm Xúc 2', N'Hoạt hình, Gia đình', 96, N'Kelsey M.', N'Amy Poehler, Maya Hawke', NULL, 13, N'Hành trình hỗn loạn khi những cảm xúc mới xuất hiện trong tâm trí cô bé Riley.', N'camxuc.jpg', N'https://youtube.com/trailer_insideout2', CAST(N'2026-05-29' AS Date), CAST(N'2025-12-31' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN019', N'Kung Fu Panda: Hiệp Sĩ Rồng', N'Hoạt hình, Phiêu lưu', 94, N'Mike M.', N'Jack Black', NULL, 13, N'Po trở lại với chuyến phiêu lưu mới để bảo vệ thung lũng khỏi mối đe dọa mới.', N'kungfupanda.jpg', N'https://youtube.com/trailer_kfp4', CAST(N'2025-11-20' AS Date), CAST(N'2026-01-05' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN020', N'Deadpool & Wolverine', N'Hành động, Siêu anh hùng', 128, N'Shawn L.', N'Ryan Reynolds, Hugh Jackman', NULL, 18, N'Sự kết hợp bùng nổ giữa hai siêu anh hùng lầy lội nhất vũ trụ.', N'deadpool.jpg', N'https://youtube.com/trailer_dp_wv', CAST(N'2025-11-05' AS Date), CAST(N'2026-06-25' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN021', N'Godzilla x Kong: Đế Chế Mới', N'Hành động, Quái vật', 115, N'Adam W.', N'Dan Stevens, Rebecca Hall', NULL, 13, N'Hai quái thú huyền thoại đối đầu với mối nguy hiểm mới của Trái Đất.', N'panda.jpg', N'https://youtube.com/trailer_gxk', CAST(N'2026-05-26' AS Date), CAST(N'2025-12-29' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN022', N'Oppenheimer: Kỷ Nguyên Hạt Nhân', N'Tâm lý, Lịch sử', 180, N'Christopher Nolan', N'Cillian Murphy, Robert Downey Jr.', NULL, 16, N'Câu chuyện xoay quanh cuộc đời nhà vật lý tạo ra vũ khí nguy hiểm nhất lịch sử.', N'kynguyenhatnhan.jpg', N'https://youtube.com/trailer_opp', CAST(N'2026-06-18' AS Date), CAST(N'2025-12-31' AS Date), N'Sắp chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN023', N'Barbie: Thế Giới Mới', N'Hài, Phiêu lưu', 114, N'Greta G.', N'Margot Robbie, Ryan Gosling', NULL, 13, N'Barbie rời quê hương để khám phá thế giới thật đầy bất ngờ.', N'barbie.jpg', N'https://youtube.com/trailer_barbie', CAST(N'2026-06-18' AS Date), CAST(N'2026-01-20' AS Date), N'Sắp chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN024', N'Sonic 3: Tốc Độ Ánh Sáng', N'Hành động, Phiêu lưu', 110, N'Jeff F.', N'Ben Schwartz, Idris Elba', NULL, 13, N'Sonic đối mặt kẻ thù mới mạnh nhất từ trước đến nay.', N'sonic3.jpg', N'https://youtube.com/trailer_sonic3', CAST(N'2026-06-18' AS Date), CAST(N'2026-01-22' AS Date), N'Sắp chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN025', N'Joker: Nụ Cười Cuối', N'Tâm lý, Tội phạm', 132, N'Todd Phillips', N'Joaquin Phoenix, Lady Gaga', NULL, 18, N'Câu chuyện đen tối về Joker trong một thế giới hỗn loạn.', N'joker.jpg', N'https://youtube.com/trailer_joker2', CAST(N'2025-11-08' AS Date), CAST(N'2026-06-28' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN026', N'Avatar 3: Biển Xanh Pandora', N'Viễn tưởng, Phiêu lưu', 175, N'James Cameron', N'Sam Worthington, Zoe Saldana', NULL, 13, N'Hành trình khám phá bộ lạc mới và hiểm họa bao trùm Pandora.', N'avatar3.jpg', N'https://youtube.com/trailer_avatar3', CAST(N'2026-06-18' AS Date), CAST(N'2026-01-25' AS Date), N'Sắp chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN027', N'Moana: Huyền Thoại Đại Dương', N'Hoạt hình, Phiêu lưu', 102, N'David D.', N'Auli’i Cravalho', NULL, 0, N'Moana tiếp tục chuyến phiêu lưu ra đại dương để tìm lại di sản của mình.', N'moana.jpg', N'https://youtube.com/trailer_moana2', CAST(N'2025-11-30' AS Date), CAST(N'2026-01-10' AS Date), N'Đang chiếu')
INSERT [dbo].[PHIM] ([MaPhim], [TenPhim], [TheLoai], [ThoiLuong], [DaoDien], [DienVien], [Nguon], [GioiHanTuoi], [MoTa], [AnhDaiDien], [Trailer], [NgayKhoiChieu], [NgayKetThuc], [TrangThai]) VALUES (N'VN028', N'Mission Impossible: Định Mệnh 2', N'Hành động, Gián điệp', 145, N'Christopher McQ.', N'Tom Cruise', NULL, 13, N'Ethan Hunt đối mặt nhiệm vụ nguy hiểm nhất sự nghiệp.', N'mission.jpg', N'https://youtube.com/trailer_mi8', CAST(N'2025-12-19' AS Date), CAST(N'2026-01-30' AS Date), N'Sắp chiếu')
GO
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai]) VALUES (N'PC01', N'Phòng 1', 30, N'Hoạt động')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai]) VALUES (N'PC02', N'Phòng 2', 30, N'Hoạt động')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai]) VALUES (N'PC03', N'Phòng 3', 30, N'Bảo trì')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai]) VALUES (N'PC04', N'Phòng 4', 30, N'Hoạt động')
INSERT [dbo].[PHONGCHIEU] ([MaPhong], [TenPhong], [SoLuong], [TrangThai]) VALUES (N'PC05', N'Phòng 5', 30, N'Hoạt động')
GO
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1110_01', N'VN001', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'10:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1110_02', N'VN001', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'12:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1110_03', N'VN001', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'14:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1110_04', N'VN001', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'16:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1113_01', N'VN001', N'PC01', CAST(N'2026-05-27' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1113_02', N'VN001', N'PC01', CAST(N'2026-05-27' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1113_03', N'VN001', N'PC01', CAST(N'2026-05-27' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1113_04', N'VN001', N'PC01', CAST(N'2026-05-27' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1117_01', N'VN001', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1117_02', N'VN001', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1117_03', N'VN001', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN001_1117_04', N'VN001', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1110_01', N'VN002', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1110_02', N'VN002', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1111_01', N'VN002', N'PC01', CAST(N'2026-05-25' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1111_02', N'VN002', N'PC01', CAST(N'2026-05-25' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1111_03', N'VN002', N'PC01', CAST(N'2026-05-25' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1111_04', N'VN002', N'PC01', CAST(N'2026-05-25' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1112_01', N'VN002', N'PC01', CAST(N'2026-05-26' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1112_02', N'VN002', N'PC01', CAST(N'2026-05-26' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1112_03', N'VN002', N'PC01', CAST(N'2026-05-26' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1112_04', N'VN002', N'PC01', CAST(N'2026-05-26' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1117_01', N'VN002', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1117_02', N'VN002', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1117_03', N'VN002', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1117_04', N'VN002', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1118_01', N'VN002', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1118_02', N'VN002', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1118_03', N'VN002', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_1118_04', N'VN002', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_0900_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_1115_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_1330_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_1545_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_1800_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_2015_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251119_2230_PC01', N'VN002', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_0900_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_1115_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_1330_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_1545_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_1800_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_2015_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251120_2230_PC01', N'VN002', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_0900_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_1115_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_1330_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_1545_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_1800_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_2015_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN002_20251121_2230_PC01', N'VN002', N'PC01', CAST(N'2025-11-21' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1110_01', N'VN003', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1110_02', N'VN003', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1110_03', N'VN003', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1110_04', N'VN003', N'PC01', CAST(N'2026-05-24' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1111_01', N'VN003', N'PC01', CAST(N'2026-05-25' AS Date), CAST(N'10:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1117_01', N'VN003', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1117_02', N'VN003', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1117_03', N'VN003', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN003_1117_04', N'VN003', N'PC01', CAST(N'2025-11-17' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_1118_01', N'VN016', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_1118_02', N'VN016', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'11:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_1118_03', N'VN016', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'13:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_1118_04', N'VN016', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'15:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_0900_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_1115_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_1330_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_1545_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_1800_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_2015_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251119_2230_PC01', N'VN016', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_0900_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_1115_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_1330_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_1545_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_1800_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_2015_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251120_2230_PC01', N'VN016', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_0900_PC01', N'VN016', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_1115_PC02', N'VN016', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_1330_PC03', N'VN016', N'PC03', CAST(N'2025-11-22' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_1545_PC04', N'VN016', N'PC04', CAST(N'2025-11-22' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_1800_PC05', N'VN016', N'PC05', CAST(N'2025-11-22' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_2015_PC01', N'VN016', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251122_2230_PC02', N'VN016', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_0900_PC01', N'VN016', N'PC01', CAST(N'2025-11-23' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_1115_PC02', N'VN016', N'PC02', CAST(N'2025-11-23' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_1330_PC03', N'VN016', N'PC03', CAST(N'2025-11-23' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_1545_PC04', N'VN016', N'PC04', CAST(N'2025-11-23' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_1800_PC05', N'VN016', N'PC05', CAST(N'2025-11-23' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_2015_PC01', N'VN016', N'PC01', CAST(N'2025-11-23' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251123_2230_PC02', N'VN016', N'PC02', CAST(N'2025-11-23' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_0900_PC01', N'VN016', N'PC01', CAST(N'2025-11-26' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_1115_PC02', N'VN016', N'PC02', CAST(N'2025-11-26' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_1330_PC03', N'VN016', N'PC03', CAST(N'2025-11-26' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_1545_PC04', N'VN016', N'PC04', CAST(N'2025-11-26' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_1800_PC05', N'VN016', N'PC05', CAST(N'2025-11-26' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_2015_PC01', N'VN016', N'PC01', CAST(N'2025-11-26' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251126_2230_PC02', N'VN016', N'PC02', CAST(N'2025-11-26' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_0900_PC01', N'VN016', N'PC01', CAST(N'2025-12-08' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
GO
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_1115_PC02', N'VN016', N'PC02', CAST(N'2025-12-08' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_1330_PC03', N'VN016', N'PC03', CAST(N'2025-12-08' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_1545_PC04', N'VN016', N'PC04', CAST(N'2025-12-08' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_1800_PC05', N'VN016', N'PC05', CAST(N'2025-12-08' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_2015_PC01', N'VN016', N'PC01', CAST(N'2025-12-08' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251208_2230_PC02', N'VN016', N'PC02', CAST(N'2025-12-08' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_0900_PC01', N'VN016', N'PC01', CAST(N'2025-12-09' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_1115_PC02', N'VN016', N'PC02', CAST(N'2025-12-09' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_1330_PC03', N'VN016', N'PC03', CAST(N'2025-12-09' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_1545_PC04', N'VN016', N'PC04', CAST(N'2025-12-09' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_1800_PC05', N'VN016', N'PC05', CAST(N'2025-12-09' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_2015_PC01', N'VN016', N'PC01', CAST(N'2025-12-09' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN016_20251209_2230_PC02', N'VN016', N'PC02', CAST(N'2025-12-09' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_0900_PC01', N'VN018', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_1115_PC02', N'VN018', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_1330_PC03', N'VN018', N'PC03', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_1545_PC04', N'VN018', N'PC04', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_1800_PC05', N'VN018', N'PC05', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_2015_PC01', N'VN018', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251120_2230_PC02', N'VN018', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_0900_PC01', N'VN018', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_1115_PC02', N'VN018', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_1330_PC03', N'VN018', N'PC03', CAST(N'2025-11-22' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_1545_PC04', N'VN018', N'PC04', CAST(N'2025-11-22' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_1800_PC05', N'VN018', N'PC05', CAST(N'2025-11-22' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_2015_PC01', N'VN018', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN018_20251122_2230_PC02', N'VN018', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_0900_PC01', N'VN019', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_1115_PC02', N'VN019', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_1330_PC03', N'VN019', N'PC03', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_1545_PC04', N'VN019', N'PC04', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_1800_PC05', N'VN019', N'PC05', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_2015_PC01', N'VN019', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251120_2230_PC02', N'VN019', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_0900_PC01', N'VN019', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_1115_PC02', N'VN019', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_1330_PC03', N'VN019', N'PC03', CAST(N'2025-11-22' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_1545_PC04', N'VN019', N'PC04', CAST(N'2025-11-22' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_1800_PC05', N'VN019', N'PC05', CAST(N'2025-11-22' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_2015_PC01', N'VN019', N'PC01', CAST(N'2025-11-22' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251122_2230_PC02', N'VN019', N'PC02', CAST(N'2025-11-22' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_0900_PC01', N'VN019', N'PC01', CAST(N'2025-12-05' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_1115_PC02', N'VN019', N'PC02', CAST(N'2025-12-05' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_1330_PC03', N'VN019', N'PC03', CAST(N'2025-12-05' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_1545_PC04', N'VN019', N'PC04', CAST(N'2025-12-05' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_1800_PC05', N'VN019', N'PC05', CAST(N'2025-12-05' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_2015_PC01', N'VN019', N'PC01', CAST(N'2025-12-05' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251205_2230_PC02', N'VN019', N'PC02', CAST(N'2025-12-05' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_0900_PC01', N'VN019', N'PC01', CAST(N'2025-12-06' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_1115_PC02', N'VN019', N'PC02', CAST(N'2025-12-06' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_1330_PC03', N'VN019', N'PC03', CAST(N'2025-12-06' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_1545_PC04', N'VN019', N'PC04', CAST(N'2025-12-06' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_1800_PC05', N'VN019', N'PC05', CAST(N'2025-12-06' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_2015_PC01', N'VN019', N'PC01', CAST(N'2025-12-06' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251206_2230_PC02', N'VN019', N'PC02', CAST(N'2025-12-06' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_0900_PC01', N'VN019', N'PC01', CAST(N'2025-12-07' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_1115_PC02', N'VN019', N'PC02', CAST(N'2025-12-07' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_1330_PC03', N'VN019', N'PC03', CAST(N'2025-12-07' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_1545_PC04', N'VN019', N'PC04', CAST(N'2025-12-07' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_1800_PC05', N'VN019', N'PC05', CAST(N'2025-12-07' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_2015_PC01', N'VN019', N'PC01', CAST(N'2025-12-07' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251207_2230_PC02', N'VN019', N'PC02', CAST(N'2025-12-07' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_0900_PC01', N'VN019', N'PC01', CAST(N'2025-12-09' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_1115_PC02', N'VN019', N'PC02', CAST(N'2025-12-09' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_1330_PC03', N'VN019', N'PC03', CAST(N'2025-12-09' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_1545_PC04', N'VN019', N'PC04', CAST(N'2025-12-09' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_1800_PC05', N'VN019', N'PC05', CAST(N'2025-12-09' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_2015_PC01', N'VN019', N'PC01', CAST(N'2025-12-09' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251209_2230_PC02', N'VN019', N'PC02', CAST(N'2025-12-09' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_0900_PC01', N'VN019', N'PC01', CAST(N'2026-06-10' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_1115_PC02', N'VN019', N'PC02', CAST(N'2026-06-10' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_1330_PC03', N'VN019', N'PC03', CAST(N'2026-06-10' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_1545_PC04', N'VN019', N'PC04', CAST(N'2026-06-10' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_1800_PC05', N'VN019', N'PC05', CAST(N'2026-06-10' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_2015_PC01', N'VN019', N'PC01', CAST(N'2026-06-10' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251210_2230_PC02', N'VN019', N'PC02', CAST(N'2026-06-10' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_0900_PC01', N'VN019', N'PC01', CAST(N'2025-12-11' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_1115_PC02', N'VN019', N'PC02', CAST(N'2025-12-11' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_1330_PC03', N'VN019', N'PC03', CAST(N'2025-12-11' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_1545_PC04', N'VN019', N'PC04', CAST(N'2025-12-11' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_1800_PC05', N'VN019', N'PC05', CAST(N'2025-12-11' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_2015_PC01', N'VN019', N'PC01', CAST(N'2025-12-11' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN019_20251211_2230_PC02', N'VN019', N'PC02', CAST(N'2025-12-11' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_0900_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_1115_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_1330_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_1545_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_1800_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_2015_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251118_2230_PC01', N'VN022', N'PC01', CAST(N'2025-11-18' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_0900_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_1115_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_1330_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_1545_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_1800_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_2015_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251119_2230_PC01', N'VN022', N'PC01', CAST(N'2025-11-19' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_0900_PC01', N'VN022', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_1115_PC02', N'VN022', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_1330_PC03', N'VN022', N'PC03', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
GO
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_1545_PC04', N'VN022', N'PC04', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_1800_PC05', N'VN022', N'PC05', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_2015_PC01', N'VN022', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251120_2230_PC02', N'VN022', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_0900_PC01', N'VN022', N'PC01', CAST(N'2025-11-23' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_1115_PC02', N'VN022', N'PC02', CAST(N'2025-11-23' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_1330_PC03', N'VN022', N'PC03', CAST(N'2025-11-23' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_1545_PC04', N'VN022', N'PC04', CAST(N'2025-11-23' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_1800_PC05', N'VN022', N'PC05', CAST(N'2025-11-23' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_2015_PC01', N'VN022', N'PC01', CAST(N'2025-11-23' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN022_20251123_2230_PC02', N'VN022', N'PC02', CAST(N'2025-11-23' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_0900_PC01', N'VN025', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_1115_PC02', N'VN025', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_1330_PC03', N'VN025', N'PC03', CAST(N'2025-11-20' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_1545_PC04', N'VN025', N'PC04', CAST(N'2025-11-20' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_1800_PC05', N'VN025', N'PC05', CAST(N'2025-11-20' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_2015_PC01', N'VN025', N'PC01', CAST(N'2025-11-20' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251120_2230_PC02', N'VN025', N'PC02', CAST(N'2025-11-20' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_0900_PC01', N'VN025', N'PC01', CAST(N'2025-11-25' AS Date), CAST(N'09:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_1115_PC02', N'VN025', N'PC02', CAST(N'2025-11-25' AS Date), CAST(N'11:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_1330_PC03', N'VN025', N'PC03', CAST(N'2025-11-25' AS Date), CAST(N'13:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_1545_PC04', N'VN025', N'PC04', CAST(N'2025-11-25' AS Date), CAST(N'15:45:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_1800_PC05', N'VN025', N'PC05', CAST(N'2025-11-25' AS Date), CAST(N'18:00:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_2015_PC01', N'VN025', N'PC01', CAST(N'2025-11-25' AS Date), CAST(N'20:15:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'S_VN025_20251125_2230_PC02', N'VN025', N'PC02', CAST(N'2025-11-25' AS Date), CAST(N'22:30:00' AS Time), CAST(50000 AS Decimal(18, 0)), NULL)
INSERT [dbo].[SUATCHIEU] ([MaSuatChieu], [MaPhim], [MaPhong], [NgayChieu], [GioBatDau], [GiaVe], [TrangThai]) VALUES (N'SC20251120215247', N'VN026', N'PC02', CAST(N'2026-01-14' AS Date), CAST(N'13:12:00' AS Time), CAST(50000 AS Decimal(18, 0)), N'Active')
GO
SET IDENTITY_INSERT [dbo].[SuKien] ON 

INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (1, N'Chương trình tích điểm thành viên 2025', N'Tăng tỷ lệ tích điểm cho vé và F&B lên đến 10% dành cho hạng Platinum.', N'sk1.jpg', CAST(N'2025-06-29' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (2, N'Combo Bánh Mì Que chỉ 50.000đ', N'Thưởng thức bánh mì que pate chà bông, xúc xích phô mai thơm ngon tại rạp.', N'sk2.jpg', CAST(N'2025-06-29' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (3, N'Trải nghiệm Super Plex với âm thanh Dolby Atmos', N'Khám phá công nghệ màn hình khổng lồ và âm thanh rạp phim đỉnh cao.', N'sk3.jpg', CAST(N'2025-06-29' AS Date), CAST(N'2026-06-22' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (4, N'Ưu đãi tháng 12 - Mua 2 vé tặng 1 bắp', N'Áp dụng tại toàn hệ thống rạp từ 01/08 đến 31/08/2025.', N'sk4.jpg', CAST(N'2025-08-01' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (5, N'Tuần lễ phim Hàn Quốc 2025', N'Khám phá 10 bộ phim Hàn đặc sắc tại các rạp lớn trên toàn quốc.', N'sk5.jpg', CAST(N'2025-07-10' AS Date), CAST(N'2025-12-31' AS Date))
INSERT [dbo].[SuKien] ([MaSK], [TieuDe], [MoTa], [Anh], [NgayBatDau], [NgayKetThuc]) VALUES (6, N'Happy Tuesday – Vé chỉ 55.000đ', N'Giảm giá đặc biệt vào mỗi thứ 3 hàng tuần.', N'sk6.jpg', CAST(N'2025-06-01' AS Date), CAST(N'2025-12-31' AS Date))
SET IDENTITY_INSERT [dbo].[SuKien] OFF
GO
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992682804651806', NULL, CAST(150000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T20:44:40.467' AS DateTime), N'VNPAY', N'TXN251120134440438', N'15273373', N'NCB', N'ATM', CAST(N'2025-11-20T20:45:05.000' AS DateTime), N'00', N'00', N'294cfe2eb317d9232563793338ae95182149b8e3d1b907c325362b23ef123df4851d8701e1e0b9cb4b50c2f3640891764f6cfc9f225226f12b235f9b96cae9de')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992692488742534', NULL, CAST(175000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:00:48.873' AS DateTime), N'VNPAY', N'TXN251120140048776', N'15273407', N'NCB', N'ATM', CAST(N'2025-11-20T21:01:21.000' AS DateTime), N'00', N'00', N'39e9d5f90264b21a223bd19c450a3b60d75108dc2f52b47fff4ab802ddc5fa366f7edf5461ad0f2795961038abc8250cb698a0b59cd18e0dc861660197d206f6')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992693683282512', NULL, CAST(145000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:02:48.327' AS DateTime), N'VNPAY', N'TXN251120140248317', N'15273415', N'NCB', N'ATM', CAST(N'2025-11-20T21:03:37.000' AS DateTime), N'00', N'00', N'6c7a95360abafd8336dc633227cbe2e7c3fbc51504ab617132ec82964b511d6ee5b5f926a5c0c7f2f09b642cdae4c27460ca2676e4c8a70ea2c6ba1faf4d2667')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992699769639718', NULL, CAST(230000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:12:56.963' AS DateTime), N'VNPAY', N'TXN251120141256937', N'15273442', N'NCB', N'ATM', CAST(N'2025-11-20T21:13:50.000' AS DateTime), N'00', N'00', N'b5c1f2de1c58c898d6e5de95bc04d8c37772f72d9da14eb6d3462566daee7fb9dd9c9c1c898ebbf7706ff022da13cf1b32ad35674ba0d4167725150e7baba8a3')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992702264363744', NULL, CAST(150000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:17:06.437' AS DateTime), N'VNPAY', N'TXN251120141706430', N'15273452', N'NCB', N'ATM', CAST(N'2025-11-20T21:17:42.000' AS DateTime), N'00', N'00', N'1f974a29799f663215efc6a0c0da7b94ffc13fa72f3f1643ae24e46a80a5bd5964f32230dced9c6fca95f864a59eb07a0119fd80f5c60d7561db8eb17b206207')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992704856765104', NULL, CAST(225000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:21:25.677' AS DateTime), N'VNPAY', N'TXN251120142125668', N'15273459', N'NCB', N'ATM', CAST(N'2025-11-20T21:22:28.000' AS DateTime), N'00', N'00', N'e07354e72e3c6eba214ea123a32c83eafbd391f8f0b49924f90fe5a0a7900d856fdfe6b7236dbc98cc3e5a0e70f9ac770415b0abf6bf9486d70bae2c41fcc6e7')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT638992707789476027', NULL, CAST(255000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-11-20T21:26:18.947' AS DateTime), N'VNPAY', N'TXN251120142618879', N'15273470', N'NCB', N'ATM', CAST(N'2025-11-20T21:27:38.000' AS DateTime), N'00', N'00', N'00894378874f74fcc57da78c89c26a442aa1b8620a97a6f3592d280857ea0751d748bb06fab43b8ac4bd1563720bd50657a4d6269d3e10c73591acd4bc736a96')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639005646070584629', NULL, CAST(150000 AS Decimal(18, 0)), N'VNPAY', N'Pending', CAST(N'2025-12-05T20:50:07.060' AS DateTime), N'VNPAY', N'TXN251205135007031', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639005646073921182', NULL, CAST(150000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-12-05T20:50:07.393' AS DateTime), N'VNPAY', N'TXN251205135007384', N'15322554', N'NCB', N'ATM', CAST(N'2025-12-05T20:50:42.000' AS DateTime), N'00', N'00', N'bb3771da9e2eaf962b1b6a2d61bd69706a9f9404eee6aa643727525ce1d2a89361b6cbb081445242326d02793b30f54f5db09d1d3bacfc0ee7a48dfffaee5354')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639005703817789937', NULL, CAST(120000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-12-05T22:26:21.780' AS DateTime), N'VNPAY', N'TXN251205152621712', N'15322714', N'NCB', N'ATM', CAST(N'2025-12-05T22:26:41.000' AS DateTime), N'00', N'00', N'377947044544e4f91134c8220517eb18e3ecd4650157af58344c08b68517303330f81c2fb94787179c03818f5ccb6b02c04ebdac6a34cffd1b0205bda1812561')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639008316831987016', NULL, CAST(175000 AS Decimal(18, 0)), N'VNPAY', N'Pending', CAST(N'2025-12-08T23:01:23.200' AS DateTime), N'VNPAY', N'TXN251208160123168', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639008691827305170', NULL, CAST(150000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2025-12-09T09:26:22.730' AS DateTime), N'VNPAY', N'TXN251209022622706', N'15328854', N'NCB', N'ATM', CAST(N'2025-12-09T09:29:03.000' AS DateTime), N'00', N'00', N'd77c10dd263efa2784a4abfe418f3b2dcc9a2a919abd65f75f85513845332c44824037e06298d4ccf3dc823d37d55deb22161215d3516786e1f39556ee06c70c')
INSERT [dbo].[THANHTOAN] ([MaTT], [MaVe], [SoTien], [HinhThuc], [TrangThai], [NgayDat], [PaymentGateway], [GatewayTxnRef], [GatewayTransNo], [BankCode], [CardType], [PayDate], [VnpResponseCode], [VnpTransactionStatus], [SecureHash]) VALUES (N'TT639009959798835893', NULL, CAST(260000 AS Decimal(18, 0)), N'VNPAY', N'Thành công', CAST(N'2026-06-10T20:39:39.883' AS DateTime), N'VNPAY', N'TXN251210133939866', N'15333186', N'NCB', N'ATM', CAST(N'2026-06-10T20:40:08.000' AS DateTime), N'00', N'00', N'00053db1ee3611774247fb2353ef7727c64f608221d4de36418286f3a9e1fb43f5dfb965466c46b7a595fe1fa6b2fb3d79e5f2d02c2863716afb35b5b51d3de3')
GO
SET IDENTITY_INSERT [dbo].[TheLoai] ON 

INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (1, N'Hành Động', N'Pha hành động, đua xe, chiến đấ', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), CAST(N'2026-06-10T13:46:52.5628433' AS DateTime2))
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (2, N'Hài Kịch', N'Hài hước, giải trí nhẹ nhàng', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (3, N'Kinh Dị', N'Yếu tố hù dọa, giật gân', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (4, N'Viễn Tưởng', N'Khoa học giả tưởng, tương lai', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (5, N'Tình Cảm', N'Romance, melodrama', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (6, N'Phiêu Lưu', N'Hành trình khám phá', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
INSERT [dbo].[TheLoai] ([MaTL], [TenTheLoai], [MoTa], [CreatedAt], [UpdatedAt]) VALUES (8, N'Tâm Lý', N'Khai thác chiều sâu tâm lý', CAST(N'2026-05-26T16:00:27.8370319' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[TheLoai] OFF
GO
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE083C05', N'KH20251118203033366', N'S_VN016_20251209_1115_PC02', N'D3', CAST(80000 AS Decimal(18, 0)), N'Pending', CAST(N'2025-12-08T23:01:23.183' AS DateTime), N'TXN251208160123168')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE3FC102', N'KH1679797060', N'S_VN025_20251125_1330_PC03', N'D2', CAST(50000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:21:25.670' AS DateTime), N'TXN251120142125668')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE43B150', N'KH1679797060', N'S_VN022_20251123_1800_PC05', N'D6', CAST(50000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:02:48.323' AS DateTime), N'TXN251120140248317')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE4E7313', N'KH20251118203033366', N'S_VN019_20251209_1330_PC03', N'C3', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-12-09T09:26:22.717' AS DateTime), N'TXN251209022622706')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE6469A1', N'KH1679797060', N'S_VN016_20251123_1545_PC04', N'D3', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:12:56.943' AS DateTime), N'TXN251120141256937')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE6A343C', N'KH20251118203033366', N'S_VN019_20251205_2230_PC02', N'D5', CAST(50000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-12-05T22:26:21.713' AS DateTime), N'TXN251205152621712')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE714B4F', N'KH1679797060', N'S_VN016_20251123_1545_PC04', N'D4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:12:56.953' AS DateTime), N'TXN251120141256937')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE778174', N'KH1679797060', N'S_VN022_20251123_1545_PC04', N'C4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:00:48.783' AS DateTime), N'TXN251120140048776')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE7FB4BE', N'KH1679797060', N'S_VN025_20251125_1330_PC03', N'D3', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:21:25.673' AS DateTime), N'TXN251120142125668')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE8DFBDE', N'KH20251118203033366', N'S_VN019_20251205_2230_PC02', N'E4', CAST(80000 AS Decimal(18, 0)), N'Pending', CAST(N'2025-12-05T20:50:07.037' AS DateTime), N'TXN251205135007031')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE9C61B1', N'KH1679797060', N'S_VN016_20251122_1330_PC03', N'D4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T20:44:40.443' AS DateTime), N'TXN251120134440438')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VE9EFF37', N'KH20251118203033366', N'S_VN019_20251205_2230_PC02', N'E4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-12-05T20:50:07.390' AS DateTime), N'TXN251205135007384')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VED41AB2', N'KH1679797060', N'S_VN018_20251122_1330_PC03', N'C4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:17:06.433' AS DateTime), N'TXN251120141706430')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VEE2851C', N'KH1679797060', N'S_VN022_20251123_1545_PC04', N'D3', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:26:18.883' AS DateTime), N'TXN251120142618879')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VEEBA849', N'KH1679797060', N'S_VN022_20251123_1545_PC04', N'D4', CAST(80000 AS Decimal(18, 0)), N'Paid', CAST(N'2025-11-20T21:26:18.940' AS DateTime), N'TXN251120142618879')
INSERT [dbo].[VE] ([MaVe], [MaKH], [MaSuatChieu], [MaGhe], [GiaVe], [TrangThai], [NgayDat], [GatewayTxnRef]) VALUES (N'VEFFD8ED', N'KH20251118203033366', N'S_VN019_20251211_1115_PC02', N'D2', CAST(50000 AS Decimal(18, 0)), N'Paid', CAST(N'2026-06-10T20:39:39.870' AS DateTime), N'TXN251210133939866')
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
