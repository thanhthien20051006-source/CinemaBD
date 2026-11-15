-- B6 durable account lockout columns.
-- This script is generated for manual review only. It has NOT been applied to any database.
-- Apply only through the separately approved migration procedure after a verified backup.
ALTER TABLE [dbo].[Admin]
    ADD [AccessFailedCount] int NULL,
        [LockoutEndUtc] datetime2 NULL;
GO

ALTER TABLE [dbo].[KHACHHANG]
    ADD [AccessFailedCount] int NULL,
        [LockoutEndUtc] datetime2 NULL;
GO

-- Rollback (run only if explicitly approved):
-- ALTER TABLE [dbo].[Admin] DROP COLUMN [AccessFailedCount], [LockoutEndUtc];
-- ALTER TABLE [dbo].[KHACHHANG] DROP COLUMN [AccessFailedCount], [LockoutEndUtc];
