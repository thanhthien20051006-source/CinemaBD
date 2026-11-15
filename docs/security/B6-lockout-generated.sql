BEGIN TRANSACTION;
GO

ALTER TABLE [Admin] ADD [AccessFailedCount] int NULL;
GO

ALTER TABLE [Admin] ADD [LockoutEndUtc] datetime2 NULL;
GO

ALTER TABLE [KHACHHANG] ADD [AccessFailedCount] int NULL;
GO

ALTER TABLE [KHACHHANG] ADD [LockoutEndUtc] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261115034812_B6DurableAccountLockout', N'8.0.12');
GO

COMMIT;
GO

