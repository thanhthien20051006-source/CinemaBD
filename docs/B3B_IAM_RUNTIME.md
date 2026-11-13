# B3b IAM runtime status

## Scope and safety

B3b implements the runtime session/token features on top of the B3 schema. The device metadata migration is present as code only and has **not** been applied. No database access, `dotnet ef database update`, or `dotnet run` was used in this change set. B4 is out of scope.

## Access JWT and sessions

- Normal access JWT lifetime: 15 minutes; JWT carries a GUID `sid` claim.
- Every authenticated API request checks the `IdentitySessions` row for a matching, unrevoked, unexpired session (and retains the existing JTI revocation check); the decision is not cached.
- Missing, duplicate, or malformed `sid` is rejected. Existing 12-hour tokens without `sid` therefore need a fresh login.
- Successful password login, registration, and Google login create a session; customer account type is `KHACHHANG`, admin type is `Admin`.
- Session listing/revocation endpoints are account-scoped. Logout revokes current session and refresh credentials; password changes revoke sessions for that account.
- `IdentitySessions.DeviceName` (100 chars) and `UserAgent` (512 chars) are bounded, untrusted display metadata. Migration `20261113110000_B3bSessionDeviceMetadata` is generated only; not applied.

## Refresh tokens

- Refresh values use 32 random bytes; storage contains only the SHA-256 digest.
- Rotation uses one conditional SQL `UPDATE` matching a still-unconsumed, unrevoked and unexpired credential/session. It requires exactly one affected row; replacement insert is committed in the same relational transaction.
- A consumed-token reuse revokes the complete session and records an audit event without raw token/hash material.
- InMemory tests cover service outcomes, but do not prove SQL Server atomicity, lock behavior, transaction behavior, or affected-row behavior under concurrency. Those remain unverified because database testing was explicitly prohibited.

## Admin permission claims

Admin login claims are sourced from current database relations: `Admin.MaCV` → active `ChucVu` (`IsMaster`) → assigned `PhanQuyen` permission names. The API keeps both a claim-based fail-closed permission filter and the existing current-database permission check, so current role changes take effect for protected admin routes rather than relying on stale JWT grants alone.

## Client compatibility

Auth response envelopes preserve existing access-token fields and add `refreshToken` and `sessionId`. Android and MVC Web clients now pass/display bounded device labels, store refresh credentials, retry a 401 once via refresh where wired, and call server logout. Legacy JWTs still require login again.
