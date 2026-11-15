-- Generated rollback for migration 20261115034812_B6DurableAccountLockout only.
-- Migration Down contains only four DropColumn operations. The EF script additionally
-- removes this migration's history row; it does not revert earlier migrations.
BEGIN TRANSACTION;
GO
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Admin]') AND [c].[name] = N'AccessFailedCount');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Admin] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Admin] DROP COLUMN [AccessFailedCount];
GO
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Admin]') AND [c].[name] = N'LockoutEndUtc');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Admin] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Admin] DROP COLUMN [LockoutEndUtc];
GO
DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[KHACHHANG]') AND [c].[name] = N'AccessFailedCount');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [KHACHHANG] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [KHACHHANG] DROP COLUMN [AccessFailedCount];
GO
DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[KHACHHANG]') AND [c].[name] = N'LockoutEndUtc');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [KHACHHANG] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [KHACHHANG] DROP COLUMN [LockoutEndUtc];
GO
DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261115034812_B6DurableAccountLockout';
GO
COMMIT;
GO
