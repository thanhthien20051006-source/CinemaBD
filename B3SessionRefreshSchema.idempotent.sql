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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE TABLE [SecurityAudit] (
        [AuditId] bigint NOT NULL IDENTITY,
        [OccurredAtUtc] datetime2 NOT NULL,
        [EventType] nvarchar(64) NOT NULL,
        [AccountType] nvarchar(32) NULL,
        [AccountId] nvarchar(128) NULL,
        [SessionId] uniqueidentifier NULL,
        [SourceIp] nvarchar(45) NULL,
        [CorrelationId] nvarchar(128) NULL,
        [Details] nvarchar(1000) NULL,
        CONSTRAINT [PK_SecurityAudit] PRIMARY KEY ([AuditId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_IdentitySessions_AccountType_AccountId_RevokedAtUtc] ON [IdentitySessions] ([AccountType], [AccountId], [RevokedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_IdentitySessions_ExpiresAtUtc] ON [IdentitySessions] ([ExpiresAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_ReplacedByRefreshTokenId] ON [RefreshTokens] ([ReplacedByRefreshTokenId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_SessionId_ExpiresAtUtc] ON [RefreshTokens] ([SessionId], [ExpiresAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_SecurityAudit_EventType_OccurredAtUtc] ON [SecurityAudit] ([EventType], [OccurredAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
    CREATE INDEX [IX_SecurityAudit_OccurredAtUtc] ON [SecurityAudit] ([OccurredAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112174026_B3SessionRefreshSchema'
)
BEGIN
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
    IF COL_LENGTH(N'[IdentitySessions]', N'DeviceName') IS NULL
    BEGIN
        ALTER TABLE [IdentitySessions] ADD [DeviceName] nvarchar(100) NULL;
    END;
    IF COL_LENGTH(N'[IdentitySessions]', N'UserAgent') IS NULL
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
    IF COL_LENGTH(N'[IdentitySessions]', N'LastSeenIp') IS NULL
    BEGIN
        ALTER TABLE [IdentitySessions] ADD [LastSeenIp] nvarchar(45) NULL;
    END;
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114000000_B3cSessionLastSeenIp', N'8.0.12');
END;
GO

COMMIT;
GO

