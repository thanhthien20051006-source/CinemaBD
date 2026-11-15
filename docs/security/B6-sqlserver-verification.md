# B6 SQL Server verification — operator-run on `CinemaBD_verify`

> Documentation only. This scenario has not been executed. Do not point it at `CinemaBD` or another operational database. The user will run it against the isolated `CinemaBD_verify` database after reviewing/applying the DDL and deploying the corresponding API build.

## Preconditions and safety

1. Create/restore an isolated SQL Server database named `CinemaBD_verify`; verify connection string `Initial Catalog` exactly matches it. Do not enable automatic migrations as part of this test.
2. Take and verify a backup/snapshot. Ensure B6 columns and B3/B5 schema are present using approved migration/DDL steps. No DDL is run by this checklist.
3. Configure test API against `CinemaBD_verify`; prevent concurrent users and scheduled jobs from modifying it. Keep secrets in environment/secret manager.
4. Prepare: (a) one Master Admin with no MFA enrollment, (b) one authorized MFA-enabled admin with `phanquyen` for response operations, (c) a disposable test customer, and (d) an isolated API process whose restart is controlled by the operator.
5. Record UTC start/end, API build/commit, SQL Server version, safe account IDs, HTTP status codes, and audit CorrelationIds. Never record credentials, TOTP codes, recovery codes, tokens, or secret values.

## A. Unenrolled Master is denied before MFA enrollment

1. Sign in with the Master account that has not enrolled MFA. Complete only the initial login step to obtain its allowed enrollment-only session/token as supported by the current login flow.
2. Before MFA enrollment, call `GET /api/admin/roles` with that bearer token.
3. **Expected:** HTTP `403 Forbidden`; no role data is returned. The enrollment-only token should remain restricted to its documented enrollment/logout/session/password-change endpoints.
4. Enroll and confirm MFA through the approved setup flow, then sign in again and complete MFA. **Expected:** a fresh normal admin token is issued only after successful verification.

## B. Revoke all sessions for a customer

1. Log the disposable customer in from two separate sessions/devices and capture their session IDs locally for verification only.
2. Use the authorized MFA-verified admin with `phanquyen` or Master access to call:
   `POST /api/admin/security/accounts/sessions/revoke-all`
   body: `{"accountType":"KHACHHANG","accountId":"<test-customer-id>"}`
3. **Expected:** HTTP `200`; audit event `admin_sessions_revoked`; both customer sessions fail the next authenticated request with `401`; refresh attempts fail and audit contains safe event metadata only.
4. Confirm an admin without `phanquyen`/Master or without an `mfa_verified` token claim gets `403` for the same operation.

## C. Lock and unlock an account

1. With the same MFA-verified authorized admin, call `POST /api/admin/security/accounts/lock` using `{"accountType":"KHACHHANG","accountId":"<test-customer-id>","locked":true}`.
2. **Expected:** HTTP `200`, `AccessFailedCount` is at least the configured threshold and `LockoutEndUtc` is set; all target sessions are revoked; audit event `admin_account_locked`.
3. Attempt customer login. **Expected:** generic login failure; no account-existence disclosure.
4. Call the same endpoint with `"locked":false`. **Expected:** HTTP `200`, both lockout fields are cleared and audit event `admin_account_unlocked`; customer can authenticate again using valid credentials.
5. Attempt to lock the only active, unlocked Master account. **Expected:** HTTP `409 Conflict`; Master remains unlocked. Do not test by risking the only live Master in a shared database.

## D. Force password change and revoke sessions

1. Create a session for the disposable account, then call `POST /api/admin/security/accounts/require-password-change` with its account type and ID.
2. **Expected:** HTTP `200`, `MustChangePassword=true`, all prior sessions/refresh credentials revoked, and audit event `admin_password_change_required`.
3. Sign in again. **Expected:** the resulting token is restricted to the password-change flow until the password is changed.

## E. Persistent lockout after API restart

1. For a disposable account, submit the configured number of incorrect passwords (`Security:AccountLockout:MaxFailedAttempts`; default 5) before the lockout duration elapses.
2. **Expected:** `AccessFailedCount` and `LockoutEndUtc` are persisted on the correct `Admin` or `KHACHHANG` row, and login remains locked.
3. Restart only the API process (do not recreate/reset the SQL Server database). Attempt login again before expiry.
4. **Expected:** lockout still applies after restart; the state is read from SQL Server, not process memory. After expiry, confirm the next valid login works and clears failed-attempt state.

## F. Closeout

- Verify expected audit event types, timestamps, account/session references, and CorrelationIds; confirm logs/audit do not contain passwords, raw JWTs, refresh tokens, OTP/recovery codes, or secret material.
- Remove/disable disposable test accounts and restore the verification database from the pre-test snapshot or discard the isolated database after retaining approved evidence.
- Record actual results and deviations. Mark each SQL Server check as complete only after it has truly been run.
