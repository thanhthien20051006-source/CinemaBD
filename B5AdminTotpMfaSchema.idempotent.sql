IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524131837_InitialCinemaBdBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524131837_InitialCinemaBdBaseline', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE TABLE [IdentitySessions] (
        [SessionId] uniqueidentifier NOT NULL,
        [AccountType] nvarchar(32) NOT NULL,
        [AccountId] nvarchar(128) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastSeenAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [RevocationReason] nvarchar(128) NULL,
        CONSTRAINT [PK_IdentitySessions] PRIMARY KEY ([SessionId])
    );

    CREATE TABLE [RefreshTokens] (
        [RefreshTokenId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [TokenHash] binary(32) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [ConsumedAtUtc] datetime2 NULL,
        [RevokedAtUtc] datetime2 NULL,
        [ReplacedByRefreshTokenId] uniqueidentifier NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([RefreshTokenId]),
        CONSTRAINT [FK_RefreshTokens_IdentitySessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [IdentitySessions] ([SessionId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RefreshTokens_RefreshTokens_ReplacedByRefreshTokenId] FOREIGN KEY ([ReplacedByRefreshTokenId]) REFERENCES [RefreshTokens] ([RefreshTokenId])
    );

    CREATE TABLE [SecurityAudit] (
        [AuditId] bigint NOT NULL IDENTITY,
        [EventType] nvarchar(64) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [AccountType] nvarchar(32) NULL,
        [AccountId] nvarchar(128) NULL,
        [SourceIp] nvarchar(45) NULL,
        [SessionId] uniqueidentifier NULL,
        [CorrelationId] nvarchar(128) NULL,
        [Details] nvarchar(1000) NULL,
        CONSTRAINT [PK_SecurityAudit] PRIMARY KEY ([AuditId])
    );

    CREATE INDEX [IX_IdentitySessions_AccountType_AccountId_RevokedAtUtc] ON [IdentitySessions] ([AccountType], [AccountId], [RevokedAtUtc]);
    CREATE INDEX [IX_IdentitySessions_ExpiresAtUtc] ON [IdentitySessions] ([ExpiresAtUtc]);
    CREATE INDEX [IX_RefreshTokens_ReplacedByRefreshTokenId] ON [RefreshTokens] ([ReplacedByRefreshTokenId]);
    CREATE INDEX [IX_RefreshTokens_SessionId_ExpiresAtUtc] ON [RefreshTokens] ([SessionId], [ExpiresAtUtc]);
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
    CREATE INDEX [IX_SecurityAudit_EventType_OccurredAtUtc] ON [SecurityAudit] ([EventType], [OccurredAtUtc]);
    CREATE INDEX [IX_SecurityAudit_OccurredAtUtc] ON [SecurityAudit] ([OccurredAtUtc]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112174026_B3SessionRefreshSchema', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113110000_B3bSessionDeviceMetadata'
)
BEGIN
    IF NOT EXISTS (
        SELECT * FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[IdentitySessions]') AND name = 'DeviceName'
    )
    BEGIN
        ALTER TABLE [IdentitySessions] ADD [DeviceName] nvarchar(100) NULL;
    END;

    IF NOT EXISTS (
        SELECT * FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[IdentitySessions]') AND name = 'UserAgent'
    )
    BEGIN
        ALTER TABLE [IdentitySessions] ADD [UserAgent] nvarchar(512) NULL;
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113110000_B3bSessionDeviceMetadata', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114000000_B3cSessionLastSeenIp'
)
BEGIN
    IF NOT EXISTS (
        SELECT * FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[IdentitySessions]') AND name = 'LastSeenIp'
    )
    BEGIN
        ALTER TABLE [IdentitySessions] ADD [LastSeenIp] nvarchar(45) NULL;
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114000000_B3cSessionLastSeenIp', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

-- =========================================================================
-- B5: Schema Multi-Factor Authentication (TOTP) cho Quản trị viên (Admin)
-- Bảng AdminMfa: Lưu trữ Secret TOTP đã được mã hóa bằng ASP.NET Core Data Protection
-- và bước thời gian LastUsedStep để ngăn chặn Replay Attack.
-- Bảng AdminRecoveryCodes: Lưu trữ băm SHA-256 (32 bytes) của mã khôi phục sử dụng 1 lần.
-- =========================================================================
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114120000_B5AdminTotpMfaSchema'
)
BEGIN
    IF OBJECT_ID(N'[AdminMfa]', N'U') IS NULL
    BEGIN
        CREATE TABLE [AdminMfa] (
            [AdminID] int NOT NULL,
            [SecretProtected] varbinary(max) NOT NULL,
            [Enabled] bit NOT NULL,
            [EnabledAtUtc] datetime2 NULL,
            [LastUsedStep] bigint NULL,
            CONSTRAINT [PK_AdminMfa] PRIMARY KEY ([AdminID]),
            CONSTRAINT [FK_AdminMfa_Admin_AdminID] FOREIGN KEY ([AdminID]) REFERENCES [Admin] ([AdminID]) ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'[AdminRecoveryCodes]', N'U') IS NULL
    BEGIN
        CREATE TABLE [AdminRecoveryCodes] (
            [Id] int IDENTITY(1,1) NOT NULL,
            [AdminID] int NOT NULL,
            [CodeHash] binary(32) NOT NULL,
            [UsedAtUtc] datetime2 NULL,
            CONSTRAINT [PK_AdminRecoveryCodes] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_AdminRecoveryCodes_Admin_AdminID] FOREIGN KEY ([AdminID]) REFERENCES [Admin] ([AdminID]) ON DELETE CASCADE
        );

        CREATE INDEX [IX_AdminRecoveryCodes_AdminID] ON [AdminRecoveryCodes] ([AdminID]);
        CREATE INDEX [IX_AdminRecoveryCodes_CodeHash] ON [AdminRecoveryCodes] ([CodeHash]);
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114120000_B5AdminTotpMfaSchema', N'8.0.12');
END;
GO

COMMIT;
GO
