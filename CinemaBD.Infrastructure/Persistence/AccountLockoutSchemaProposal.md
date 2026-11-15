# Account lockout schema proposal (not applied)

This proposal is deliberately separate from the EF model and database scripts. No migration was generated or applied for lockout storage.

```sql
ALTER TABLE [dbo].[Admin] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;
ALTER TABLE [dbo].[KHACHHANG] ADD [AccessFailedCount] int NULL, [LockoutEndUtc] datetime2 NULL;
```

Before using this schema, approve a migration, review exact SQL Server table/column compatibility against the target schema, and implement durable atomic increments and lockout expiry behavior. Current B4 lockout policy state is process-local in-memory, so it resets on application restart and is not shared across multiple API instances. The proposed fields are nullable, matching the requested migration-only design.
