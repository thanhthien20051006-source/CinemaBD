using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CinemaBD.Infrastructure.Security;
using System.Text.RegularExpressions;

namespace CinemaBD.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing required configuration ConnectionStrings__DefaultConnection.");

        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection must include Database/Initial Catalog.");
        var allowAutoMigrate = _configuration.GetValue<bool>("Database:AllowAutoMigrate", false);
        var allowedNames = _configuration.GetSection("Database:AllowedNames").Get<string[]>() ?? Array.Empty<string>();
        var databaseAllowed = !string.IsNullOrWhiteSpace(databaseName)
            && allowedNames.Any(name => string.Equals(name?.Trim(), databaseName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!allowAutoMigrate || !databaseAllowed)
        {
            _logger.LogWarning(
                "Database initialization skipped for {DatabaseName}: Database:AllowAutoMigrate is {AllowAutoMigrate}; database allowlisted={DatabaseAllowed}.",
                string.IsNullOrWhiteSpace(databaseName) ? "<unspecified>" : databaseName,
                allowAutoMigrate,
                databaseAllowed);
            return;
        }

        var allowDemoData = _configuration.GetValue<bool>("DatabaseSeed:AllowDemoData", false);
        var envName = _configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isDevelopment = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase);

        if (allowDemoData && !isDevelopment)
        {
            _logger.LogWarning("CANH BAO AN NINH: DatabaseSeed:AllowDemoData dang duoc bat (true) trong moi truong ngoai Development ({Environment}). Du lieu demo va tai khoan mac dinh co the gay rui ro an ninh!", envName);
        }

        if (!_db.Database.IsRelational())
        {
            _logger.LogInformation("Database provider khong phai Relational (vi du InMemory test). Bo qua ExecuteSqlRaw.");
            return;
        }

        var hasTables = await CanConnectAndHasTablesAsync(cancellationToken);
        if (!hasTables)
        {
            await InitializeEmptyDatabaseAsync(connectionString, databaseName, allowDemoData, cancellationToken);
        }

        await EnsureBaselineMigrationHistoryAsync(cancellationToken);
        await ApplyPendingMigrationsIfAvailableAsync(cancellationToken);
        await EnsureApplicationSchemaAsync(cancellationToken);
        await EnsureRolesAndPermissionsAsync(cancellationToken);
        await EnsureBootstrapAdminAsync(cancellationToken);
        await UpgradeLegacyPlaintextPasswordsAsync(cancellationToken);
        await EnsureDemoDataAsync(allowDemoData, cancellationToken);
        if (allowDemoData)
        {
            await EnsureLoyaltyPointsAsync(cancellationToken);
            await EnsureUpcomingShowtimesAsync(cancellationToken);
        }
    }

    private async Task InitializeEmptyDatabaseAsync(string connectionString, string databaseName, bool allowDemoData, CancellationToken cancellationToken)
    {
        var schemaPath = ResolveSchemaSqlPath();
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Không tìm thấy file schema SQL: {schemaPath}", schemaPath);

        _logger.LogInformation("Database {DatabaseName} chưa có bảng. Đang tạo schema từ {SchemaPath}", databaseName, schemaPath);
        await ExecuteSqlScriptAsync(connectionString, databaseName, schemaPath, cancellationToken);
        _logger.LogInformation("Khởi tạo schema database {DatabaseName} hoàn tất.", databaseName);

        if (allowDemoData)
        {
            var dataPath = ResolveDataSqlPath();
            if (File.Exists(dataPath))
            {
                _logger.LogInformation("DatabaseSeed:AllowDemoData = true. Đang nạp dữ liệu mẫu từ {DataPath}", dataPath);
                await ExecuteSqlScriptAsync(connectionString, databaseName, dataPath, cancellationToken);
                _logger.LogInformation("Nạp dữ liệu mẫu cho database {DatabaseName} hoàn tất.", databaseName);
            }
            else
            {
                var legacySqlPath = ResolveSeedSqlPath();
                if (File.Exists(legacySqlPath))
                {
                    _logger.LogInformation("Đang nạp dữ liệu từ file legacy {SqlPath}", legacySqlPath);
                    await ExecuteSqlScriptAsync(connectionString, databaseName, legacySqlPath, cancellationToken);
                }
            }
        }
        else
        {
            _logger.LogInformation("DatabaseSeed:AllowDemoData = false. Bỏ qua nạp dữ liệu mẫu.");
        }
    }

    public static readonly (string MigrationId, string WitnessSql)[] DefaultWitnesses = new[]
    {
        (
            "20260524131837_InitialCinemaBdBaseline",
            // The baseline migration is empty (Up does nothing), so we mark it
            // applied when any application tables exist at all.
            "OBJECT_ID(N'[dbo].[KHACHHANG]', N'U') IS NOT NULL"
        ),
        (
            "20261112174026_B3SessionRefreshSchema",
            // Witness: IdentitySessions table created by this migration.
            "OBJECT_ID(N'[dbo].[IdentitySessions]', N'U') IS NOT NULL"
        ),
        (
            "20261113110000_B3bSessionDeviceMetadata",
            // Witness: DeviceName column added to IdentitySessions.
            "OBJECT_ID(N'[dbo].[IdentitySessions]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[IdentitySessions]', N'DeviceName') IS NOT NULL"
        ),
        (
            "20261114000000_B3cSessionLastSeenIp",
            // Witness: LastSeenIp column added to IdentitySessions.
            "OBJECT_ID(N'[dbo].[IdentitySessions]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[IdentitySessions]', N'LastSeenIp') IS NOT NULL"
        ),
        (
            "20261115034812_B6DurableAccountLockout",
            // Witness: durable lockout fields are present on both legacy account tables.
            "OBJECT_ID(N'[dbo].[Admin]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[Admin]', N'AccessFailedCount') IS NOT NULL AND COL_LENGTH(N'[dbo].[Admin]', N'LockoutEndUtc') IS NOT NULL AND OBJECT_ID(N'[dbo].[KHACHHANG]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[KHACHHANG]', N'AccessFailedCount') IS NOT NULL AND COL_LENGTH(N'[dbo].[KHACHHANG]', N'LockoutEndUtc') IS NOT NULL"
        ),
    };

    /// <summary>
    /// Pure SQL generation for idempotently creating __EFMigrationsHistory and recording baseline migrations.
    /// Used by EnsureBaselineMigrationHistoryAsync and unit tests.
    /// </summary>
    public static string BuildBaselineMigrationHistorySql(
        IEnumerable<string> allMigrations,
        string? productVersion = null,
        IEnumerable<(string MigrationId, string WitnessSql)>? customWitnesses = null)
    {
        var efVersion = typeof(DbContext).Assembly.GetName().Version;
        productVersion ??= efVersion is null ? "8.0.0" : $"{efVersion.Major}.{efVersion.Minor}.{efVersion.Build}";

        var witnesses = customWitnesses ?? DefaultWitnesses;
        var witnessMap = witnesses.ToDictionary(w => w.MigrationId, w => w.WitnessSql, StringComparer.Ordinal);
        var migrationsToCheck = new List<string>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(@"
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory]
    (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;");

        foreach (var migrationId in allMigrations)
        {
            if (!witnessMap.TryGetValue(migrationId, out var witnessSql))
            {
                // No witness defined — this is a future migration that should be
                // applied normally by MigrateAsync. Do NOT insert a baseline row.
                continue;
            }

            migrationsToCheck.Add(migrationId);

            sb.AppendLine($@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'{migrationId}')
   AND ({witnessSql})
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'{migrationId}', N'{productVersion}');
END;");
        }

        return migrationsToCheck.Count == 0 ? string.Empty : sb.ToString();
    }

    /// <summary>
    /// Idempotently creates the __EFMigrationsHistory table and inserts baseline
    /// rows for migrations whose schema objects already exist in the database.
    /// This prevents MigrateAsync from re-running DDL on databases created from
    /// the SQL bootstrap script (CinemaBD.schema.sql) or from an earlier version
    /// that did not use EF Core migrations.
    /// </summary>
    public async Task EnsureBaselineMigrationHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
            return;

        var allMigrations = _db.Database.GetMigrations().ToList();
        if (allMigrations.Count == 0)
            return;

        var sql = BuildBaselineMigrationHistorySql(allMigrations);
        if (string.IsNullOrWhiteSpace(sql))
            return;

        await _db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("EnsureBaselineMigrationHistory kiểm tra và cập nhật baseline migration history hoàn tất.");
    }

    private async Task ApplyPendingMigrationsIfAvailableAsync(CancellationToken cancellationToken)
    {
        var migrations = _db.Database.GetMigrations();
        if (!migrations.Any())
            return;

        var pendingMigrations = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pendingMigrations.Any())
            return;

        _logger.LogInformation("Đang áp dụng {Count} EF Core migration cho CinemaBD.", pendingMigrations.Count());
        await _db.Database.MigrateAsync(cancellationToken);
    }

    private async Task EnsureApplicationSchemaAsync(CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlRawAsync(@"
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
ELSE IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RevokedTokens_ExpiresAtUtc' AND object_id = OBJECT_ID(N'[dbo].[RevokedTokens]'))
BEGIN
    CREATE INDEX [IX_RevokedTokens_ExpiresAtUtc] ON [dbo].[RevokedTokens] ([ExpiresAtUtc]);
END;

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

IF OBJECT_ID(N'[dbo].[THANHTOAN]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'PaymentGateway') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [PaymentGateway] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'GatewayTxnRef') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [GatewayTxnRef] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'GatewayTransNo') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [GatewayTransNo] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'BankCode') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [BankCode] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'CardType') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [CardType] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'PayDate') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [PayDate] datetime2 NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'VnpResponseCode') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [VnpResponseCode] nvarchar(20) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'VnpTransactionStatus') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [VnpTransactionStatus] nvarchar(20) NULL;
    IF COL_LENGTH(N'[dbo].[THANHTOAN]', N'SecureHash') IS NULL ALTER TABLE [dbo].[THANHTOAN] ADD [SecureHash] nvarchar(max) NULL;
END;

IF OBJECT_ID(N'[dbo].[VE]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[VE]', N'GatewayTxnRef') IS NULL ALTER TABLE [dbo].[VE] ADD [GatewayTxnRef] nvarchar(50) NULL;
    IF COL_LENGTH(N'[dbo].[VE]', N'DaCheckIn') IS NULL ALTER TABLE [dbo].[VE] ADD [DaCheckIn] bit NULL;
    IF COL_LENGTH(N'[dbo].[VE]', N'ThoiGianCheckIn') IS NULL ALTER TABLE [dbo].[VE] ADD [ThoiGianCheckIn] datetime2 NULL;
END;

IF OBJECT_ID(N'[dbo].[HoaDons]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[HoaDons]', N'GatewayTxnRef') IS NULL ALTER TABLE [dbo].[HoaDons] ADD [GatewayTxnRef] nvarchar(50) NOT NULL CONSTRAINT [DF_HoaDons_GatewayTxnRef] DEFAULT N'';
    IF COL_LENGTH(N'[dbo].[HoaDons]', N'GhiChu') IS NULL ALTER TABLE [dbo].[HoaDons] ADD [GhiChu] nvarchar(max) NULL;
    IF COL_LENGTH(N'[dbo].[HoaDons]', N'DaCheckIn') IS NULL ALTER TABLE [dbo].[HoaDons] ADD [DaCheckIn] bit NULL;
    IF COL_LENGTH(N'[dbo].[HoaDons]', N'ThoiGianCheckIn') IS NULL ALTER TABLE [dbo].[HoaDons] ADD [ThoiGianCheckIn] datetime2 NULL;
END;

IF OBJECT_ID(N'[dbo].[DanhGia]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[DanhGia]', N'Rating') IS NULL ALTER TABLE [dbo].[DanhGia] ADD [Rating] int NULL;
    IF COL_LENGTH(N'[dbo].[DanhGia]', N'IsHidden') IS NULL ALTER TABLE [dbo].[DanhGia] ADD [IsHidden] bit NULL;
END;

IF OBJECT_ID(N'[dbo].[PHONGCHIEU]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[PHONGCHIEU]', N'MaRap') IS NULL ALTER TABLE [dbo].[PHONGCHIEU] ADD [MaRap] nvarchar(max) NULL;
END;

IF OBJECT_ID(N'[dbo].[HoaDons]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[HoaDons]', N'GatewayTxnRef') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HoaDons_GatewayTxnRef' AND object_id = OBJECT_ID(N'[dbo].[HoaDons]'))
BEGIN
    CREATE UNIQUE INDEX [IX_HoaDons_GatewayTxnRef] ON [dbo].[HoaDons] ([GatewayTxnRef]) WHERE [GatewayTxnRef] IS NOT NULL AND [GatewayTxnRef] <> N'';
END;

IF OBJECT_ID(N'[dbo].[VE]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[VE]', N'MaSuatChieu') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[VE]', N'MaGhe') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_VE_Showtime_Seat_Active' AND object_id = OBJECT_ID(N'[dbo].[VE]'))
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[VE]
        WHERE [TrangThai] IN (N'Paid', N'Success', N'Thành công', N'Đã thanh toán', N'CheckedIn')
        GROUP BY [MaSuatChieu], [MaGhe] HAVING COUNT(*) > 1)
BEGIN
    CREATE UNIQUE INDEX [UX_VE_Showtime_Seat_Active] ON [dbo].[VE] ([MaSuatChieu], [MaGhe])
        WHERE [TrangThai] IN (N'Paid', N'Success', N'Thành công', N'Đã thanh toán', N'CheckedIn');
END;

IF OBJECT_ID(N'[dbo].[Admin]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Admin]', N'MustChangePassword') IS NULL ALTER TABLE [dbo].[Admin] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_Admin_MustChangePassword] DEFAULT 0;
    IF COL_LENGTH(N'[dbo].[Admin]', N'PasswordExpiresAt') IS NULL ALTER TABLE [dbo].[Admin] ADD [PasswordExpiresAt] datetime2 NULL;
END;

IF OBJECT_ID(N'[dbo].[KHACHHANG]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[KHACHHANG]', N'MustChangePassword') IS NULL ALTER TABLE [dbo].[KHACHHANG] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_KHACHHANG_MustChangePassword] DEFAULT 0;
    IF COL_LENGTH(N'[dbo].[KHACHHANG]', N'PasswordExpiresAt') IS NULL ALTER TABLE [dbo].[KHACHHANG] ADD [PasswordExpiresAt] datetime2 NULL;
END;", cancellationToken);
    }

    public async Task UpgradeLegacyPlaintextPasswordsAsync(CancellationToken cancellationToken = default)
    {
        var passwordHasher = new Pbkdf2PasswordHasher();

        var customers = await _db.Customers
            .Where(x => !string.IsNullOrEmpty(x.MatKhau))
            .ToListAsync(cancellationToken);

        var upgradedCustomersCount = 0;
        foreach (var customer in customers)
        {
            if (IsPlaintextPassword(customer.MatKhau))
            {
                customer.MatKhau = passwordHasher.Hash(customer.MatKhau);
                customer.MustChangePassword = true;
                customer.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
                upgradedCustomersCount++;
            }
        }

        var admins = await _db.Admins
            .Where(x => !string.IsNullOrEmpty(x.Password))
            .ToListAsync(cancellationToken);

        var upgradedAdminsCount = 0;
        foreach (var admin in admins)
        {
            if (IsPlaintextPassword(admin.Password))
            {
                admin.Password = passwordHasher.Hash(admin.Password);
                admin.MustChangePassword = true;
                admin.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
                upgradedAdminsCount++;
            }
        }

        if (upgradedCustomersCount > 0 || upgradedAdminsCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Da tu dong nang cap mat khau plaintext len IDENTITY hash cho {CustomerCount} khach hang va {AdminCount} quan tri vien.", upgradedCustomersCount, upgradedAdminsCount);
        }
    }

    public static bool IsPlaintextPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.StartsWith("IDENTITY:", StringComparison.Ordinal))
            return false;

        if (password.StartsWith("GoogleAuth_", StringComparison.OrdinalIgnoreCase)
            || password.StartsWith("GOOGLE_", StringComparison.OrdinalIgnoreCase)
            || password.StartsWith("EXTERNAL_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(password, "NO_PASSWORD", StringComparison.OrdinalIgnoreCase))
            return false;

        if (password.Length == 32 && password.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            return false;

        return true;
    }

    public async Task EnsureRolesAndPermissionsAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucVu] WHERE [TenChucVu] = N'Quản trị hệ thống')
    INSERT INTO [dbo].[ChucVu] ([TenChucVu], [IsMaster], [IsActive], [CreatedAt]) VALUES (N'Quản trị hệ thống', 1, 1, SYSDATETIME());

DECLARE @AdminRoleId int = (SELECT TOP 1 [MaCV] FROM [dbo].[ChucVu] WHERE [TenChucVu] = N'Quản trị hệ thống' ORDER BY [MaCV]);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'dashboard') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'dashboard');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'phim') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'phim');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'lichchieu') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'lichchieu');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'hoadon') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'hoadon');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'khachhang') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'khachhang');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'dichvu') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'dichvu');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'voucher') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'voucher');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'khuyenmai') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'khuyenmai');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'thongke') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'thongke');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'nhanvien') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'nhanvien');
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChucNang] WHERE [TenChucNang] = N'phanquyen') INSERT INTO [dbo].[ChucNang] ([TenChucNang]) VALUES (N'phanquyen');

INSERT INTO [dbo].[PhanQuyen] ([MaCV], [MaCN])
SELECT @AdminRoleId, cn.[MaCN]
FROM [dbo].[ChucNang] cn
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[PhanQuyen] pq WHERE pq.[MaCV] = @AdminRoleId AND pq.[MaCN] = cn.[MaCN]);
", cancellationToken);
    }

    public async Task EnsureBootstrapAdminAsync(CancellationToken cancellationToken = default)
    {
        var bootstrapUsername = _configuration["BOOTSTRAP_ADMIN_USERNAME"]
            ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_USERNAME");
        var bootstrapPassword = _configuration["BOOTSTRAP_ADMIN_PASSWORD"]
            ?? Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(bootstrapUsername) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            _logger.LogWarning("Bootstrap admin was not created because BOOTSTRAP_ADMIN_USERNAME and/or BOOTSTRAP_ADMIN_PASSWORD is missing.");
            return;
        }

        if (await _db.Admins.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Bootstrap admin creation skipped because an administrator account already exists.");
            return;
        }

        if (!IsStrongBootstrapPassword(bootstrapPassword))
        {
            throw new InvalidOperationException("BOOTSTRAP_ADMIN_PASSWORD must be at least 12 characters and include uppercase, lowercase, numeric, and non-alphanumeric characters.");
        }

        var adminRoleId = await _db.Roles.AsNoTracking()
            .Where(x => x.TenChucVu == "Quản trị hệ thống" && x.IsMaster)
            .Select(x => (int?)x.MaCV)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId is null or 0)
            throw new InvalidOperationException("The master administrator role is missing; bootstrap admin was not created.");

        var passwordHasher = new Pbkdf2PasswordHasher();
        var hashedPassword = passwordHasher.Hash(bootstrapPassword);

        var newAdmin = new LegacyAdmin
        {
            Username = bootstrapUsername.Trim(),
            Password = hashedPassword,
            FullName = "Bootstrap Administrator",
            Email = "admin@cinemabd.local",
            Phone = "0900000001",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MaCV = adminRoleId,
            MustChangePassword = true,
            PasswordExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Admins.Add(newAdmin);
        await _db.SaveChangesAsync(cancellationToken);

        // TUYỆT ĐỐI KHÔNG GHI LOG MẬT KHẨU
        _logger.LogInformation("Bootstrap administrator account was created and must be rotated by the operator.");
    }

    private static bool IsStrongBootstrapPassword(string password)
    {
        return password.Length >= 12
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => !char.IsLetterOrDigit(character));
    }

    private async Task EnsureDemoDataAsync(bool allowDemoData, CancellationToken cancellationToken)
    {
        if (allowDemoData)
        {
            await EnsureAdminDemoDataAsync(cancellationToken);
            await EnsureCustomerDemoDataAsync(cancellationToken);
            await EnsureCatalogDemoDataAsync(cancellationToken);
            await EnsureBusinessDemoDataAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("DatabaseSeed:AllowDemoData = false. Bỏ qua nạp dữ liệu mẫu demo.");
        }
    }

    private async Task EnsureAdminDemoDataAsync(CancellationToken cancellationToken)
    {
        var passwordHasher = new Pbkdf2PasswordHasher();
        var hashedAdminPassword = passwordHasher.Hash("admin123");

        await _db.Database.ExecuteSqlRawAsync(@"
DECLARE @AdminRoleId int = (SELECT TOP 1 [MaCV] FROM [dbo].[ChucVu] WHERE [TenChucVu] = N'Quản trị hệ thống' ORDER BY [MaCV]);

IF NOT EXISTS (SELECT 1 FROM [dbo].[NhanVien] WHERE [Email] = N'admin@cinemabd.local')
    INSERT INTO [dbo].[NhanVien] ([HoTen], [NgaySinh], [SDT], [Email], [NgayBatDau], [MaCV], [TrangThai]) VALUES (N'Quản trị viên', NULL, N'0900000001', N'admin@cinemabd.local', CONVERT(date, DATEADD(year, -1, SYSDATETIME())), @AdminRoleId, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Admin] WHERE [Username] = N'admin')
    INSERT INTO [dbo].[Admin] ([Username], [Password], [FullName], [Email], [Phone], [Role], [CreatedAt], [IsActive], [MaCV], [MustChangePassword], [PasswordExpiresAt])
    VALUES (N'admin', {0}, N'Quản trị viên', N'admin@cinemabd.local', N'0900000001', N'Admin', SYSDATETIME(), 1, @AdminRoleId, 1, DATEADD(day, 7, SYSDATETIME()));
", new object[] { hashedAdminPassword }, cancellationToken);

        var existingAdmin = await _db.Admins.FirstOrDefaultAsync(x => x.Username == "admin", cancellationToken);
        if (existingAdmin != null && passwordHasher.NeedsRehash(existingAdmin.Password))
        {
            existingAdmin.Password = hashedAdminPassword;
            existingAdmin.MustChangePassword = true;
            existingAdmin.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureCatalogDemoDataAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Cinemas.AnyAsync(x => x.MaRap == "RAP01", cancellationToken))
            _db.Cinemas.Add(new LegacyCinema { MaRap = "RAP01", TenRap = "CinemaBD Bình Dương", DiaChi = "Thủ Dầu Một, Bình Dương", SoDienThoai = "0274000000", TrangThai = "Hoạt động" });

        await _db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Hành động') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Hành động', N'Phim hành động', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Tình cảm') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Tình cảm', N'Phim tình cảm', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Hoạt hình') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Hoạt hình', N'Phim hoạt hình', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Kinh dị') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Kinh dị', N'Phim kinh dị', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Hài') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Hài', N'Phim hài', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Khoa học viễn tưởng') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Khoa học viễn tưởng', N'Phim khoa học viễn tưởng', SYSDATETIME());
IF NOT EXISTS (SELECT 1 FROM [dbo].[TheLoai] WHERE [TenTheLoai] = N'Phiêu lưu') INSERT INTO [dbo].[TheLoai] ([TenTheLoai], [MoTa], [CreatedAt]) VALUES (N'Phiêu lưu', N'Phim phiêu lưu', SYSDATETIME());
", cancellationToken);

        // Poster filenames must match files in CinemaBD.Web/wwwroot/legacy/Content/img/Posters/
        var movies = new (string Id, string Name, string Genre, int Duration, decimal Price, string Poster)[]
        {
            ("VN001", "Lật Mặt 8", "Hành động", 120, 90000m, "latmat.jpg"),
            ("VN002", "Mai", "Tình cảm", 130, 90000m, "nhabanu.jpg"),
            ("VN003", "Đất Rừng Phương Nam", "Phiêu lưu", 115, 85000m, "moana.jpg"),
            ("VN004", "Doraemon Movie", "Hoạt hình", 105, 80000m, "kungfupanda.jpg"),
            ("VN005", "Avengers Demo", "Khoa học viễn tưởng", 150, 100000m, "mission.jpg"),
            ("VN006", "Nhà Bà Nữ", "Hài", 110, 85000m, "camxuc.jpg")
        };

        foreach (var movie in movies)
        {
            var existingMovie = await _db.Movies.FirstOrDefaultAsync(x => x.MaPhim == movie.Id, cancellationToken);
            if (existingMovie == null)
            {
                _db.Movies.Add(new LegacyMovie
                {
                    MaPhim = movie.Id,
                    TenPhim = movie.Name,
                    TheLoai = movie.Genre,
                    ThoiLuong = movie.Duration,
                    DaoDien = "CinemaBD Studio",
                    DienVien = "Diễn viên demo",
                    GioiHanTuoi = 13,
                    MoTa = $"Dữ liệu demo cho phim {movie.Name}.",
                    AnhDaiDien = movie.Poster,
                    Trailer = "https://www.youtube.com/",
                    NgayKhoiChieu = DateTime.Today.AddDays(-7),
                    NgayKetThuc = DateTime.Today.AddMonths(2),
                    TrangThai = "Active"
                });
            }
            else if (string.IsNullOrWhiteSpace(existingMovie.AnhDaiDien)
                     || existingMovie.AnhDaiDien.Contains("default.jpg", StringComparison.OrdinalIgnoreCase))
            {
                existingMovie.AnhDaiDien = movie.Poster;
            }
        }

        // Sanitize any legacy movie poster paths (e.g. from CinemaBD.data.sql or older seeds)
        var allDbMovies = await _db.Movies.ToListAsync(cancellationToken);
        foreach (var m in allDbMovies)
        {
            if (string.IsNullOrWhiteSpace(m.AnhDaiDien)) continue;
            var lower = m.AnhDaiDien.ToLowerInvariant();
            if (lower.Contains("lat-mat") || lower.Contains("latmat")) m.AnhDaiDien = "latmat.jpg";
            else if (lower.Contains("deadpool")) m.AnhDaiDien = "deadpool.jpg";
            else if (lower.Contains("inside") || lower.Contains("camxuc")) m.AnhDaiDien = "camxuc.jpg";
            else if (lower.Contains("ma-da") || lower.Contains("mada") || lower.Contains("sonic")) m.AnhDaiDien = "sonic3.jpg";
            else if (lower.Contains("joker")) m.AnhDaiDien = "joker.jpg";
            else if (lower.Contains("default.jpg")) m.AnhDaiDien = "latmat.jpg";
        }

        // Combo image filenames must match files in CinemaBD.Web/wwwroot/legacy/Content/img/
        var combos = new[]
        {
            ("CB01", "Combo Bắp Nước", 70000m, "1 bắp lớn + 1 nước", "combo1.jpg"),
            ("CB02", "Combo Couple", 95000m, "1 bắp lớn + 2 nước", "combo2.jpg"),
            ("CB03", "Combo Family", 150000m, "2 bắp lớn + 4 nước", "combo3.jpg")
        };

        foreach (var combo in combos)
        {
            var existingCombo = await _db.Combos.FirstOrDefaultAsync(x => x.MaCombo == combo.Item1, cancellationToken);
            if (existingCombo == null)
                _db.Combos.Add(new LegacyCombo { MaCombo = combo.Item1, TenCombo = combo.Item2, Gia = combo.Item3, MoTa = combo.Item4, Anh = combo.Item5 });
            else if (string.IsNullOrWhiteSpace(existingCombo.Anh) || existingCombo.Anh.Contains("default.jpg", StringComparison.OrdinalIgnoreCase) || existingCombo.Anh.Contains("/images/"))
                existingCombo.Anh = combo.Item5;
        }

        // Sanitize any existing combos
        var allDbCombos = await _db.Combos.ToListAsync(cancellationToken);
        foreach (var c in allDbCombos)
        {
            if (string.IsNullOrWhiteSpace(c.Anh)) continue;
            var lower = c.Anh.ToLowerInvariant();
            if (lower.Contains("combo1")) c.Anh = "combo1.jpg";
            else if (lower.Contains("combo2")) c.Anh = "combo2.jpg";
            else if (lower.Contains("combo3")) c.Anh = "combo3.jpg";
        }

        if (!await _db.Articles.AnyAsync(cancellationToken))
            _db.Articles.Add(new LegacyArticle { TieuDe = "CinemaBD chào mừng bạn", MoTa = "Tin tức demo", NoiDung = "Nội dung demo cho trang bài viết.", Anh = "img/news/top-bomtan-2025.jpg", NgayDang = DateTime.Now });
        if (!await _db.Events.AnyAsync(cancellationToken))
            _db.Events.Add(new LegacyEvent { TieuDe = "Tuần lễ phim Việt", MoTa = "Sự kiện demo", Anh = "img/skien/sk1.jpg", NgayBatDau = DateTime.Today, NgayKetThuc = DateTime.Today.AddDays(7) });

        var allDbArticles = await _db.Articles.ToListAsync(cancellationToken);
        foreach (var a in allDbArticles)
        {
            if (string.IsNullOrWhiteSpace(a.Anh)) continue;
            if (!a.Anh.StartsWith("img/", StringComparison.OrdinalIgnoreCase) && !a.Anh.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(a.Anh);
                a.Anh = $"img/news/{fileName}";
            }
        }

        var allDbEvents = await _db.Events.ToListAsync(cancellationToken);
        foreach (var ev in allDbEvents)
        {
            if (string.IsNullOrWhiteSpace(ev.Anh)) continue;
            var lower = ev.Anh.ToLowerInvariant();
            if (lower.Contains("tuesday") || lower.Contains("sk1")) ev.Anh = "img/skien/sk1.jpg";
            else if (lower.Contains("hssv") || lower.Contains("sk2")) ev.Anh = "img/skien/sk2.jpg";
            else if (lower.Contains("halloween") || lower.Contains("sk3")) ev.Anh = "img/skien/sk3.jpg";
            else if (!ev.Anh.StartsWith("img/", StringComparison.OrdinalIgnoreCase) && !ev.Anh.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(ev.Anh);
                ev.Anh = $"img/skien/{fileName}";
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCustomerDemoDataAsync(CancellationToken cancellationToken)
    {
        var passwordHasher = new Pbkdf2PasswordHasher();
        var hashedDemoPassword = passwordHasher.Hash("123456");

        var customers = new[]
        {
            ("KHDEMO01", "Nguyễn Văn Demo", "0901000001", "demo1@cinemabd.local", "demo1", hashedDemoPassword),
            ("KHDEMO02", "Trần Thị Cinema", "0901000002", "demo2@cinemabd.local", "demo2", hashedDemoPassword),
            ("KHDEMO03", "Lê Minh Khách", "0901000003", "demo3@cinemabd.local", "demo3", hashedDemoPassword)
        };

        foreach (var customer in customers)
        {
            var existing = await _db.Customers.FirstOrDefaultAsync(x => x.MaKH == customer.Item1, cancellationToken);
            if (existing == null)
            {
                _db.Customers.Add(new LegacyCustomer
                {
                    MaKH = customer.Item1,
                    HoTen = customer.Item2,
                    SDT = customer.Item3,
                    Email = customer.Item4,
                    TKhoan = customer.Item5,
                    MatKhau = customer.Item6,
                    NgaySinh = new DateTime(2000, 1, 1),
                    MustChangePassword = true,
                    PasswordExpiresAt = DateTime.UtcNow.AddDays(7)
                });
            }
            else if (passwordHasher.NeedsRehash(existing.MatKhau))
            {
                existing.MatKhau = customer.Item6;
                existing.MustChangePassword = true;
                existing.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBusinessDemoDataAsync(CancellationToken cancellationToken)
    {
        var voucherSeeds = new[]
        {
            new LegacyVoucher { MaVoucher = "VCSALE20", MaKH = "ALL", MaCode = "SALE20", MoTa = "Giảm 20% cho mọi khách hàng", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 20, LoaiGiam = "Percent", DonToiThieu = 0, GiamToiDa = 100000, DaSuDung = false },
            new LegacyVoucher { MaVoucher = "VCGLOBAL50", MaKH = "ALL", MaCode = "GIAM50", MoTa = "Giảm 50.000 đ cho mọi khách hàng", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 50000, LoaiGiam = "Amount", DonToiThieu = 100000, GiamToiDa = 0, DaSuDung = false },
            new LegacyVoucher { MaVoucher = "VCGLOBAL10", MaKH = "ALL", MaCode = "GIAM10", MoTa = "Giảm 10% tối đa 30.000 đ", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 10, LoaiGiam = "Percent", DonToiThieu = 0, GiamToiDa = 50000, DaSuDung = false },
            new LegacyVoucher { MaVoucher = "VCGLOBAL20", MaKH = "ALL", MaCode = "GIAM20", MoTa = "Giảm 20% cho mọi khách hàng", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 20, LoaiGiam = "Percent", DonToiThieu = 0, GiamToiDa = 100000, DaSuDung = false },
            new LegacyVoucher { MaVoucher = "VCSALE10", MaKH = "ALL", MaCode = "SALE10", MoTa = "Giảm 10% cho mọi khách hàng", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 10, LoaiGiam = "Percent", DonToiThieu = 0, GiamToiDa = 50000, DaSuDung = false },
            new LegacyVoucher { MaVoucher = "VCKHDEMO01", MaKH = "KHDEMO01", MaCode = "VIP20", MoTa = "Voucher riêng khách demo", NgayHetHan = DateTime.Today.AddMonths(12), GiaTriGiam = 20, LoaiGiam = "Percent", DonToiThieu = 150000, GiamToiDa = 50000, DaSuDung = false }
        };

        foreach (var voucher in voucherSeeds)
        {
            if (!await _db.Vouchers.AnyAsync(x => x.MaCode == voucher.MaCode, cancellationToken))
                _db.Vouchers.Add(voucher);
        }

        if (!await _db.Payments.AnyAsync(x => x.GatewayTxnRef == "TXNDEMO0001", cancellationToken))
            _db.Payments.Add(new LegacyPayment { MaTT = "TTDEMO0001", MaVe = null, SoTien = 160000, HinhThuc = "VNPAY", TrangThai = "Thành công", NgayDat = DateTime.Now.AddDays(-1), PaymentGateway = "VNPAY", GatewayTxnRef = "TXNDEMO0001", GatewayTransNo = "DEMO0001", BankCode = "NCB", CardType = "ATM", PayDate = DateTime.Now.AddDays(-1), VnpResponseCode = "00", VnpTransactionStatus = "00" });

        if (!await _db.Tickets.AnyAsync(x => x.MaVe == "VEDEMO0001", cancellationToken))
            _db.Tickets.Add(new LegacyTicket { MaVe = "VEDEMO0001", MaKH = "KHDEMO01", MaSuatChieu = await ResolveDemoShowtimeIdAsync(cancellationToken), MaGhe = "A1", GiaVe = 90000, TrangThai = "Paid", NgayDat = DateTime.Now.AddDays(-1), GatewayTxnRef = "TXNDEMO0001" });

        if (!await _db.BookedCombos.AnyAsync(x => x.GatewayTxnRef == "TXNDEMO0001" && x.MaCombo == "CB01", cancellationToken))
            _db.BookedCombos.Add(new LegacyBookedCombo { GatewayTxnRef = "TXNDEMO0001", MaCombo = "CB01", SoLuong = 1, Gia = 70000 });

        if (!await _db.Invoices.AnyAsync(x => x.MaHD == "HDDEMO0001", cancellationToken))
            _db.Invoices.Add(new LegacyInvoice { MaHD = "HDDEMO0001", MaKH = "KHDEMO01", GatewayTxnRef = "TXNDEMO0001", TongTien = 160000, NgayThanhToan = DateTime.Now.AddDays(-1), TrangThai = "Đã thanh toán", GhiChu = "Hóa đơn demo đồng bộ dữ liệu" });

        await _db.SaveChangesAsync(cancellationToken);

        if (!await _db.InvoiceLineItems.AnyAsync(x => x.MaHD == "HDDEMO0001" && x.MaVe == "VEDEMO0001", cancellationToken))
            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem { MaHD = "HDDEMO0001", LoaiDong = "Ticket", MaVe = "VEDEMO0001", TenDong = "Vé xem phim demo", SoLuong = 1, DonGia = 90000 });
        if (!await _db.InvoiceLineItems.AnyAsync(x => x.MaHD == "HDDEMO0001" && x.MaCombo == "CB01", cancellationToken))
            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem { MaHD = "HDDEMO0001", LoaiDong = "Combo", MaCombo = "CB01", TenDong = "Combo Bắp Nước", SoLuong = 1, DonGia = 70000 });

        if (!await _db.InvoiceHistories.AnyAsync(x => x.MaHD == "HDDEMO0001", cancellationToken))
            _db.InvoiceHistories.Add(new LegacyInvoiceHistory { MaHD = "HDDEMO0001", TrangThai = "Đã thanh toán", ThoiGian = DateTime.Now.AddDays(-1), GhiChu = "Tạo hóa đơn demo" });

        if (!await _db.RefundRequests.AnyAsync(x => x.MaHD == "HDDEMO0001", cancellationToken))
            _db.RefundRequests.Add(new LegacyRefundRequest { MaHD = "HDDEMO0001", MaVe = "VEDEMO0001", MaKH = "KHDEMO01", Reason = "Yêu cầu hoàn tiền demo", Status = "Pending", RequestedAt = DateTime.Now, RefundAmount = 90000 });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveDemoShowtimeIdAsync(CancellationToken cancellationToken)
    {
        var showtimeId = await _db.Showtimes.AsNoTracking()
            .Where(x => x.NgayChieu.Date >= DateTime.Today && x.TrangThai != "Cancelled" && x.TrangThai != "Expired")
            .OrderBy(x => x.NgayChieu)
            .ThenBy(x => x.GioBatDau)
            .Select(x => x.MaSuatChieu)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(showtimeId))
            return showtimeId;

        var movieId = await _db.Movies.AsNoTracking().OrderBy(x => x.MaPhim).Select(x => x.MaPhim).FirstAsync(cancellationToken);
        var roomId = await _db.Rooms.AsNoTracking().OrderBy(x => x.MaPhong).Select(x => x.MaPhong).FirstAsync(cancellationToken);
        showtimeId = "DEMO" + DateTime.Today.ToString("yyyyMMdd") + "0800";
        _db.Showtimes.Add(new LegacyShowtime { MaSuatChieu = showtimeId, MaPhim = movieId, MaPhong = roomId, NgayChieu = DateTime.Today.AddDays(1), GioBatDau = new TimeSpan(8, 0, 0), GiaVe = 90000, TrangThai = "Active" });
        await _db.SaveChangesAsync(cancellationToken);
        return showtimeId;
    }

    private async Task EnsureLoyaltyPointsAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Customers.AnyAsync(cancellationToken))
            return;

        var customerIds = await _db.Customers.AsNoTracking()
            .Select(x => x.MaKH)
            .ToListAsync(cancellationToken);
        var existingIds = await _db.LoyaltyPoints.AsNoTracking()
            .Select(x => x.MaKH)
            .ToListAsync(cancellationToken);
        var existingSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var customerId in customerIds.Where(id => !existingSet.Contains(id)))
        {
            _db.LoyaltyPoints.Add(new LegacyLoyaltyPoints
            {
                MaTichDiem = "TD" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                MaKH = customerId,
                DiemThuong = 0,
                DiemCong = 0,
                DiemTru = 0
            });
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUpcomingShowtimesAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Movies.AnyAsync(cancellationToken))
            return;

        var now = DateTime.Now;
        var today = now.Date;
        const int daysToEnsure = 7;
        const int minShowsPerMoviePerDay = 3;
        const int maxShowsPerMoviePerDay = 6;
        const int desiredRoomCount = 20;
        const int cleanupMinutes = 20;
        var firstStartTime = new TimeSpan(8, 0, 0);
        var closeTime = new TimeSpan(23, 59, 0);

        await EnsureRoomsAsync(desiredRoomCount, cancellationToken);
        var targetDates = Enumerable.Range(0, daysToEnsure).Select(offset => today.AddDays(offset)).ToList();

        await RepairInvalidShowtimesAsync(today, today.AddDays(daysToEnsure - 1), maxShowsPerMoviePerDay, cancellationToken);

        var movies = await _db.Movies.AsNoTracking()
            .Where(m => m.TrangThai == null || m.TrangThai != "Inactive")
            .OrderBy(m => m.MaPhim)
            .Select(m => new { m.MaPhim, m.ThoiLuong })
            .ToListAsync(cancellationToken);

        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.TrangThai == null || r.TrangThai != "Ngưng hoạt động")
            .OrderBy(r => r.MaPhong)
            .Select(r => r.MaPhong)
            .Take(desiredRoomCount)
            .ToListAsync(cancellationToken);

        if (movies.Count == 0 || rooms.Count == 0)
            return;

        var defaultPrice = await _db.Showtimes.AsNoTracking()
            .Where(s => s.GiaVe > 0)
            .Select(s => (decimal?)s.GiaVe)
            .FirstOrDefaultAsync(cancellationToken) ?? 90000m;

        var created = 0;
        foreach (var targetDate in targetDates)
        {
            var allDateShowtimes = await _db.Showtimes.AsNoTracking()
                .Where(s => s.NgayChieu.Date == targetDate)
                .Select(s => new { s.MaSuatChieu, s.MaPhim, s.MaPhong, s.GioBatDau, s.TrangThai })
                .ToListAsync(cancellationToken);

            var existingShowtimes = allDateShowtimes
                .Where(s => s.TrangThai != "Cancelled" && s.TrangThai != "Expired")
                .Select(s => new { s.MaSuatChieu, s.MaPhim, s.MaPhong, s.GioBatDau })
                .ToList();

            var visibleExistingShowtimes = targetDate == today
                ? existingShowtimes.Where(s => s.GioBatDau > now.TimeOfDay).ToList()
                : existingShowtimes;

            var existingIdSet = allDateShowtimes.Select(s => s.MaSuatChieu).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingCounts = visibleExistingShowtimes
                .GroupBy(s => s.MaPhim)
                .ToDictionary(g => g.Key, g => Math.Min(g.Count(), maxShowsPerMoviePerDay), StringComparer.OrdinalIgnoreCase);

            var occupied = rooms.ToDictionary(room => room, _ => new List<(TimeSpan Start, TimeSpan End)>(), StringComparer.OrdinalIgnoreCase);
            foreach (var showtime in visibleExistingShowtimes.Where(s => occupied.ContainsKey(s.MaPhong)))
            {
                var movie = movies.FirstOrDefault(m => string.Equals(m.MaPhim, showtime.MaPhim, StringComparison.OrdinalIgnoreCase));
                var duration = movie?.ThoiLuong > 0 ? movie.ThoiLuong : 120;
                occupied[showtime.MaPhong].Add((showtime.GioBatDau, showtime.GioBatDau.Add(TimeSpan.FromMinutes(duration + cleanupMinutes))));
            }

            foreach (var movie in movies)
            {
                var duration = movie.ThoiLuong > 0 ? movie.ThoiLuong : 120;
                var candidateSlots = BuildStartSlots(firstStartTime, closeTime, duration)
                    .Where(t => targetDate > today || t > now.TimeOfDay)
                    .ToList();

                while (existingCounts.GetValueOrDefault(movie.MaPhim) < minShowsPerMoviePerDay)
                {
                    var placed = false;
                    foreach (var startTime in candidateSlots)
                    {
                        var endWithCleanup = startTime.Add(TimeSpan.FromMinutes(duration + cleanupMinutes));
                        var roomId = rooms.FirstOrDefault(room => !occupied[room].Any(x => startTime < x.End && endWithCleanup > x.Start));
                        if (roomId == null)
                            continue;

                        var showtimeId = BuildShowtimeId(movie.MaPhim, roomId, targetDate, startTime);
                        if (existingIdSet.Contains(showtimeId))
                            continue;

                        _db.Showtimes.Add(new LegacyShowtime
                        {
                            MaSuatChieu = showtimeId,
                            MaPhim = movie.MaPhim,
                            MaPhong = roomId,
                            NgayChieu = targetDate,
                            GioBatDau = startTime,
                            GiaVe = defaultPrice,
                            TrangThai = "Active"
                        });

                        existingIdSet.Add(showtimeId);
                        existingCounts[movie.MaPhim] = existingCounts.GetValueOrDefault(movie.MaPhim) + 1;
                        occupied[roomId].Add((startTime, endWithCleanup));
                        created++;
                        placed = true;
                        break;
                    }

                    if (!placed)
                    {
                        _logger.LogWarning("Không đủ phòng/khung giờ để tạo tối thiểu {Target} suất cho phim {MovieId} ngày {Date}.", minShowsPerMoviePerDay, movie.MaPhim, targetDate.ToString("yyyy-MM-dd"));
                        break;
                    }
                }
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã tự sinh {Count} suất chiếu hợp lệ cho {Days} ngày tới: mỗi phim {Min}-{Max} suất/ngày, tối đa {Rooms} phòng.", created, daysToEnsure, minShowsPerMoviePerDay, maxShowsPerMoviePerDay, desiredRoomCount);
        }
    }

    private static IEnumerable<TimeSpan> BuildStartSlots(TimeSpan firstStartTime, TimeSpan closeTime, int durationMinutes)
    {
        for (var start = firstStartTime; start.Add(TimeSpan.FromMinutes(durationMinutes)) <= closeTime; start = start.Add(TimeSpan.FromMinutes(30)))
            yield return start;
    }

    private async Task EnsureRoomsAsync(int desiredRoomCount, CancellationToken cancellationToken)
    {
        await NormalizeRoomsAndSeatsAsync(desiredRoomCount, cancellationToken);

        var currentRooms = await _db.Rooms.OrderBy(r => r.MaPhong).ToListAsync(cancellationToken);
        if (currentRooms.Count >= desiredRoomCount)
            return;


        var seatCount = currentRooms.FirstOrDefault(r => r.SoLuong > 0)?.SoLuong ?? 30;
        var existingRoomIds = currentRooms.Select(r => r.MaPhong).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingSeats = await _db.Seats.AsNoTracking()
            .Select(s => new { s.MaPhong, s.MaGhe })
            .ToListAsync(cancellationToken);
        var existingSeatSet = existingSeats
            .Select(s => $"{s.MaPhong}|{s.MaGhe}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createdRooms = 0;
        for (var number = 1; currentRooms.Count + createdRooms < desiredRoomCount && number <= desiredRoomCount; number++)
        {
            var roomId = $"PC{number:00}";
            if (existingRoomIds.Contains(roomId))
                continue;

            _db.Rooms.Add(new LegacyRoom
            {
                MaPhong = roomId,
                TenPhong = $"Phòng {number}",
                SoLuong = seatCount,
                TrangThai = "Hoạt động"
            });
            existingRoomIds.Add(roomId);
            createdRooms++;

        }

        if (createdRooms > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var entry in _db.ChangeTracker.Entries<LegacySeat>().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;

            var createdRoomIds = existingRoomIds
                .Where(id => !currentRooms.Any(r => string.Equals(r.MaPhong, id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var roomId in createdRoomIds)
            {
                foreach (var seatId in BuildSeatIds(roomId, seatCount))
                {
                    var seatKey = $"{roomId}|{seatId}";
                    if (existingSeatSet.Contains(seatKey))
                        continue;

                    _db.Seats.Add(new LegacySeat
                    {
                        MaGhe = seatId,
                        MaPhong = roomId,
                        LoaiGhe = IsVipSeat(seatId) ? "VIP" : "Thường",
                        TrangThai = "Trống"
                    });
                    existingSeatSet.Add(seatKey);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã mở rộng thêm {Count} phòng chiếu, tổng mục tiêu {Target} phòng.", createdRooms, desiredRoomCount);
        }
    }

    private async Task NormalizeRoomsAndSeatsAsync(int desiredRoomCount, CancellationToken cancellationToken)
    {
        var changed = false;

        var rooms = await _db.Rooms.ToListAsync(cancellationToken);
        foreach (var room in rooms)
        {
            if (TryGetRoomNumber(room.MaPhong, out var number) && number >= 1 && number <= desiredRoomCount)
            {
                var expectedName = $"Phòng {number}";
                if (!string.Equals(room.TenPhong, expectedName, StringComparison.Ordinal))
                {
                    room.TenPhong = expectedName;
                    changed = true;
                }
            }

            if (IsMojibake(room.TrangThai))
            {
                room.TrangThai = "Hoạt động";
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync(cancellationToken);

        await SynchronizeRoomSeatsAsync(rooms, cancellationToken);
    }

    private async Task SynchronizeRoomSeatsAsync(List<LegacyRoom> rooms, CancellationToken cancellationToken)
    {
        var standardSeats = BuildStandardSeatIds().ToList();
        var standardSet = standardSeats.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roomsById = rooms.Select(r => r.MaPhong).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seats = await _db.Seats.Where(s => roomsById.Contains(s.MaPhong)).ToListAsync(cancellationToken);
        var seatGroups = seats.GroupBy(s => s.MaPhong, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.MaGhe, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        var ticketedSeatKeys = (await (from t in _db.Tickets.AsNoTracking()
                join s in _db.Showtimes.AsNoTracking() on t.MaSuatChieu equals s.MaSuatChieu
                where roomsById.Contains(s.MaPhong)
                select new { s.MaPhong, t.MaGhe })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.MaPhong}|{x.MaGhe}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var room in rooms)
        {
            if (!seatGroups.TryGetValue(room.MaPhong, out var roomSeats))
            {
                roomSeats = new Dictionary<string, LegacySeat>(StringComparer.OrdinalIgnoreCase);
                seatGroups[room.MaPhong] = roomSeats;
            }

            foreach (var seatId in standardSeats)
            {
                var expectedType = IsVipSeat(seatId) ? "VIP" : "THUONG";
                if (!roomSeats.TryGetValue(seatId, out var seat))
                {
                    _db.Seats.Add(new LegacySeat
                    {
                        MaPhong = room.MaPhong,
                        MaGhe = seatId,
                        LoaiGhe = expectedType,
                        TrangThai = "Hoạt động"
                    });
                    changed = true;
                    continue;
                }

                if (!string.Equals(seat.LoaiGhe, expectedType, StringComparison.Ordinal))
                {
                    seat.LoaiGhe = expectedType;
                    changed = true;
                }

                if (!string.Equals(seat.TrangThai, "Hoạt động", StringComparison.Ordinal))
                {
                    seat.TrangThai = "Hoạt động";
                    changed = true;
                }
            }

            foreach (var extraSeat in roomSeats.Values.Where(s => !standardSet.Contains(s.MaGhe)).ToList())
            {
                if (ticketedSeatKeys.Contains($"{extraSeat.MaPhong}|{extraSeat.MaGhe}"))
                {
                    if (!string.Equals(extraSeat.TrangThai, "Khóa", StringComparison.Ordinal))
                    {
                        extraSeat.TrangThai = "Khóa";
                        changed = true;
                    }
                    continue;
                }

                _db.Seats.Remove(extraSeat);
                changed = true;
            }

            room.SoLuong = standardSeats.Count;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã đồng bộ sơ đồ ghế {SeatCount} ghế cho {RoomCount} phòng theo layout chuẩn.", standardSeats.Count, rooms.Count);
        }
    }

    private static bool TryGetRoomNumber(string? roomId, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(roomId) || roomId.Length < 3 || !roomId.StartsWith("PC", StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(roomId[2..], out number);
    }

    private static bool IsMojibake(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Contains('�') || value.Contains('?');

    private static string NormalizeSeatId(string seatId)
    {
        var id = (seatId ?? string.Empty).Trim().ToUpperInvariant();
        var dashIndex = id.LastIndexOf('-');
        if (dashIndex >= 0 && dashIndex < id.Length - 1)
            id = id[(dashIndex + 1)..];

        if (id.Length >= 2 && char.IsLetter(id[0]) && int.TryParse(id[1..], out var col))
            return $"{id[0]}{col}";

        return id;
    }

    private static string NormalizeSeatType(string? type, string seatId)
    {
        if (!string.IsNullOrWhiteSpace(type) && type.Equals("VIP", StringComparison.OrdinalIgnoreCase))
            return "VIP";

        return IsVipSeat(seatId) ? "VIP" : "THUONG";
    }

    private static string NormalizeSeatStatus(string? status)
    {
        if (IsMojibake(status) || string.Equals(status, "Trống", StringComparison.OrdinalIgnoreCase))
            return "Hoạt động";

        return status!.Trim();
    }

    private static IEnumerable<string> BuildSeatIds(string roomId, int seatCount)
    {
        return BuildStandardSeatIds().Take(seatCount);
    }

    private static IEnumerable<string> BuildStandardSeatIds()
    {
        foreach (var row in new[] { 'A', 'B', 'C', 'D', 'E' })
        {
            for (var col = 1; col <= 6; col++)
                yield return $"{row}{col}";
        }
    }

    private static bool IsVipSeat(string seatId)
    {
        var normalizedSeatId = NormalizeSeatId(seatId);
        return normalizedSeatId.Length >= 2
            && int.TryParse(normalizedSeatId[1..], out var col)
            && col is 3 or 4;
    }
    private async Task RepairInvalidShowtimesAsync(DateTime fromDate, DateTime toDate, int maxShowsPerMoviePerDay, CancellationToken cancellationToken)
    {
        var showtimes = await _db.Showtimes
            .Where(s => s.NgayChieu.Date >= fromDate && s.NgayChieu.Date <= toDate && s.TrangThai != "Cancelled" && s.TrangThai != "Expired")
            .OrderBy(s => s.NgayChieu)
            .ThenBy(s => s.GioBatDau)
            .ThenBy(s => s.MaPhong)
            .ToListAsync(cancellationToken);

        if (showtimes.Count == 0)
            return;

        var ids = showtimes.Select(s => s.MaSuatChieu).ToList();
        var ticketedIds = (await _db.Tickets.AsNoTracking()
                .Where(t => ids.Contains(t.MaSuatChieu))
                .Select(t => t.MaSuatChieu)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in showtimes.GroupBy(s => new { Date = s.NgayChieu.Date, s.MaPhong, s.GioBatDau }))
        {
            var ordered = group
                .OrderByDescending(s => ticketedIds.Contains(s.MaSuatChieu))
                .ThenBy(s => s.MaSuatChieu.StartsWith("AUTO", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(s => s.MaSuatChieu)
                .ToList();

            foreach (var duplicate in ordered.Skip(1).Where(s => !ticketedIds.Contains(s.MaSuatChieu)))
                toRemove.Add(duplicate.MaSuatChieu);
        }

        foreach (var group in showtimes
            .Where(s => !toRemove.Contains(s.MaSuatChieu))
            .GroupBy(s => new { Date = s.NgayChieu.Date, s.MaPhim }))
        {
            var extra = group
                .OrderBy(s => ticketedIds.Contains(s.MaSuatChieu) ? 1 : 0)
                .ThenByDescending(s => s.MaSuatChieu.StartsWith("AUTO", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(s => s.GioBatDau)
                .Skip(maxShowsPerMoviePerDay)
                .Where(s => !ticketedIds.Contains(s.MaSuatChieu));

            foreach (var showtime in extra)
                toRemove.Add(showtime.MaSuatChieu);
        }

        if (toRemove.Count == 0)
            return;

        var removable = showtimes.Where(s => toRemove.Contains(s.MaSuatChieu)).ToList();
        _db.Showtimes.RemoveRange(removable);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã xóa {Count} suất chiếu chưa có vé bị trùng phòng/giờ hoặc vượt quá giới hạn mỗi phim/ngày.", removable.Count);
    }

    private static TimeSpan RoundUpToNextSlot(TimeSpan value)
    {
        const int stepMinutes = 30;
        var totalMinutes = (int)Math.Ceiling(value.TotalMinutes / stepMinutes) * stepMinutes;
        return TimeSpan.FromMinutes(totalMinutes);
    }

    private static string BuildShowtimeId(string movieId, string roomId, DateTime date, TimeSpan startTime)
    {
        var safeMovie = Regex.Replace(movieId, "[^A-Za-z0-9]", string.Empty);
        var safeRoom = Regex.Replace(roomId, "[^A-Za-z0-9]", string.Empty);
        return $"AUTO{date:yyyyMMdd}{startTime:hhmm}{safeRoom}{safeMovie}";
    }

    private async Task<bool> CanConnectAndHasTablesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
                return false;

            var tableCount = await _db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
                .SingleAsync(cancellationToken);

            return tableCount > 0;
        }
        catch
        {
            return false;
        }
    }

    private string ResolveSchemaSqlPath()
    {
        var configuredPath = _configuration["DatabaseSeed:SchemaSqlFile"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(fullConfiguredPath))
                return fullConfiguredPath;
        }

        var primaryPath = Path.Combine(AppContext.BaseDirectory, "Database", "CinemaBD.schema.sql");
        if (File.Exists(primaryPath))
            return primaryPath;

        var fallbackPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CinemaBD.schema.sql"));
        if (File.Exists(fallbackPath))
            return fallbackPath;

        return primaryPath;
    }

    private string ResolveDataSqlPath()
    {
        var configuredPath = _configuration["DatabaseSeed:DataSqlFile"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(fullConfiguredPath))
                return fullConfiguredPath;
        }

        var primaryPath = Path.Combine(AppContext.BaseDirectory, "Database", "CinemaBD.data.sql");
        if (File.Exists(primaryPath))
            return primaryPath;

        var fallbackPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CinemaBD.data.sql"));
        if (File.Exists(fallbackPath))
            return fallbackPath;

        return primaryPath;
    }

    private string ResolveSeedSqlPath()
    {
        var configuredPath = _configuration["DatabaseSeed:SqlFile"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
            if (File.Exists(fullConfiguredPath))
                return fullConfiguredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Database", "CinemaBD.sql");
    }

    private static async Task ExecuteSqlScriptAsync(string connectionString, string databaseName, string sqlPath, CancellationToken cancellationToken)
    {
        var masterConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterConnectionBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(connection, $"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL CREATE DATABASE [{EscapeSqlIdentifier(databaseName)}];", cancellationToken);

        var script = await File.ReadAllTextAsync(sqlPath, cancellationToken);
        script = PrepareScript(script, databaseName);

        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await ExecuteNonQueryAsync(connection, batch, cancellationToken);
        }
    }

    private static string PrepareScript(string script, string databaseName)
    {
        script = Regex.Replace(script, @"(?im)^\s*IF\s+EXISTS\s*\([^\r\n]*sys\.databases[^\r\n]*\)\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*DROP\s+DATABASE\s+\[?CinemaBD\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*CREATE\s+DATABASE\s+\[?CinemaBD\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*USE\s+\[?master\]?\s*$", string.Empty);
        script = Regex.Replace(script, @"(?im)^\s*USE\s+\[?CinemaBD\]?\s*$", $"USE [{EscapeSqlIdentifier(databaseName)}]");
        return script;
    }

    private static string EscapeSqlIdentifier(string value) => value.Replace("]", "]]");

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        return Regex.Split(script, @"(?im)^\s*GO\s*;?\s*$");
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 180;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}









