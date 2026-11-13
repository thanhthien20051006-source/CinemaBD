namespace CinemaBD.Infrastructure.Persistence;

/// <summary>
/// Proposal only. Nullable fields are intentionally not mapped and are not part of the EF model.
/// The runtime lockout policy currently uses process-local in-memory counters pending approval
/// and application of a durable SQL migration.
/// </summary>
public static class AccountLockoutSchemaProposal
{
    public const string ProposedMigrationSql = "ALTER TABLE [dbo].[Admin] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;\nALTER TABLE [dbo].[KHACHHANG] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;";
}
