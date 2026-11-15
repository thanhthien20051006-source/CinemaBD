namespace CinemaBD.Infrastructure.Persistence;

/// <summary>SQL Server DDL companion for the generated, unapplied durable lockout migration.</summary>
public static class AccountLockoutSchemaProposal
{
    public const string ProposedMigrationSql = "ALTER TABLE [dbo].[Admin] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;\nALTER TABLE [dbo].[KHACHHANG] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;";
    public const string ProposedDownSql = "ALTER TABLE [dbo].[Admin] DROP COLUMN [AccessFailedCount], [LockoutEndUtc];\nALTER TABLE [dbo].[KHACHHANG] DROP COLUMN [AccessFailedCount], [LockoutEndUtc];";
}
