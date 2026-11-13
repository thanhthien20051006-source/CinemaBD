# B3 IAM session and refresh-token schema — review notes

## Scope

This B3 step adds the session/refresh/audit schema and a shared validator for audit details and revocation reasons. The migration and SQL are generated for review; they have not been applied to any database. No runtime session validation, audit writer, refresh endpoint, rotation, or reuse handling is implemented in this step.

## Decisions to implement in the runtime phase

- Issue access JWTs with a 15-minute lifetime and a `sid` claim.
- On every authenticated request, look up `sid` and require a non-revoked, unexpired session. Do not cache the active-session decision.
- Reject legacy 12-hour JWTs without `sid`; users must sign in again.
- Generate refresh tokens with sufficient cryptographic randomness; persist only SHA-256 hashes in `RefreshTokens.TokenHash`.
- Rotate refresh tokens with one conditional atomic `UPDATE` and require exactly one affected row. Do not implement this with a read-then-write sequence.
- If a consumed refresh token is presented again, revoke its entire session and write a security-audit event. Never record raw refresh tokens or their hashes in audit data or logs.

## Schema summary

- `IdentitySessions`: opaque `SessionId`, account type/id, creation/last-seen/expiry timestamps, revocation timestamp and reason.
- `RefreshTokens`: session FK, fixed-size 32-byte SHA-256 digest, expiry/consumption/revocation timestamps, replacement link.
- `SecurityAudit`: event type/time, optional account/session/source/correlation metadata and bounded detail. No token/hash column.
- Existing `RevokedTokens` remains in the model for compatibility with current JTI revocation until the later runtime migration is designed.

`IdentitySessions.AccountType` and `AccountId` are intentionally polymorphic and have no FK to the account tables because customer IDs are strings while admin IDs are integers. Runtime code must canonicalize the account type and ID consistently.

## Employee credentials — recommendation only

Prefer a separate employee credential/account table linked to `NhanVien.MaNV`. This keeps employee authentication lifecycle separate from HR profile data and avoids conflating employees with `Admin`; the tradeoff is extra provisioning and authentication mapping. Do not add employee credential schema or login behavior in B3. Authorization should map `NhanVien.MaCV` → `ChucVu` → `PhanQuyen`; decide whether permissions are dynamically evaluated or represented as claims, and ensure permission changes invalidate or refresh affected sessions/tokens.

## Runtime and operational requirements (not implemented in B3)

- **Expired-session cleanup job:** schedule a server-side job at least daily in UTC to delete sessions whose `ExpiresAtUtc` is more than 30 days in the past. The `RefreshTokens.SessionId` cascade FK removes related refresh-token records. The grace period preserves a short investigation window; tune it against incident-response and privacy requirements. The job must be bounded/batched and monitored, and must not delete audit records as a side effect. This job is a requirement for the later runtime phase; B3 only defines the schema.
- **SecurityAudit retention:** retain audit rows for 365 days, then purge or archive them according to the deployment's legal/records policy. Restrict audit access, monitor retention-job outcomes, and suspend deletion during an active investigation/legal hold. The current B3 schema has no retention job; this policy is a proposed operational baseline, not an implemented control.
- `IdentitySessions.AccountId` intentionally has **no foreign key**. It is polymorphic across customer/admin identity domains whose key types and tables differ; runtime code must canonicalize `AccountType` and `AccountId` consistently.
- Any future writer of `SecurityAudit.Details` or `IdentitySessions.RevocationReason` must pass values through `SecurityMetadataSanitizer` before persistence. It rejects JWT-like values, long base64-like strings, 64-character hex hashes, `IDENTITY:` hashes, and password/token/secret labels followed by values, then truncates safe text to the respective schema limit. B3 does not implement audit/session writers or logging. No B3 log path currently exists, so log-content confidentiality is not yet covered by runtime tests; future code must not log these raw metadata values or credentials.

## Migration caveat requiring review

The current `InitialCinemaBdBaseline` migration has empty `Up`/`Down`, while the EF snapshot describes portions of the application schema that are separately established by `CinemaBD.schema.sql` / `DatabaseInitializer`. EF therefore scaffolds some existing password-lifecycle columns and `RevokedTokens` as if they were new. The B3 delta migration has been reviewed to create only the three new B3 tables and their indexes/foreign keys; the generated idempotent script records the empty baseline when history is absent, then guards B3 operations by migration ID. This does not prove SQL Server behavior on a real legacy schema: script execution against either history scenario has not been performed. No database was changed or migrated.

The idempotent script was generated with `dotnet ef migrations script --idempotent`. Its SQL has only been inspected statically. Do not run it or `dotnet ef database update` against `CinemaBD_verify` or `CinemaBD` without separate explicit approval.
