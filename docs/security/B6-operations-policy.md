# B6 security data and alert policy

## Audit/query access

- Forensic event reads are restricted to authorized, MFA-verified Master or `phanquyen` administrators. API results are a whitelist projection of event time/type, account type/ID, session ID, CorrelationId and sanitized Details; source IP, raw token/hash fields, password/MFA secret/recovery values are not part of the response contract.
- Query filters are exact-match account, session, event type and inclusive UTC time boundaries. Pagination defaults to 50 records and is capped at 200 records/page. Record the operator and investigation reference in the incident process; do not treat this API as a substitute for immutable external evidence storage.

## Retention and cleanup

- Default proposed retention: **365 days** for SecurityAudit, aligned with the existing B3 review baseline. Deployments must obtain legal/records approval for a different duration.
- The cleanup job is enabled by default (`Security:Cleanup:Enabled=true`), runs hourly (`Security:Cleanup:IntervalMinutes=60`), and uses `Security:Audit:RetentionDays=365`. It removes audit rows older than retention, expired identity sessions and their refresh tokens, independently expired refresh tokens, and expired revoked-token records. Expired sessions are removed; sessions still within their expiry lifetime are retained even if revoked so they remain available for investigation. Only refresh-token records for expired sessions and independently expired refresh tokens are eligible for cleanup; revoked sessions still in retention keep their refresh-token rows until token expiry.
- Before an active investigation or legal hold, set `Security:Cleanup:LegalHold=true`; this suppresses cleanup. To pause cleanup for maintenance, set `Security:Cleanup:Enabled=false`. Monitor cleanup counts/failures; the job logs counts but not row details or credentials.
- Retention configuration is a deployment control, not an assertion that every environment has approved its legal policy. Restore the approved configuration after an investigation is closed.

## Alert thresholds

Configurable structured Warning alerts are evaluated from persisted SecurityAudit events; no email or external notification is sent. Defaults: `Security:Alerts:FailedLoginCount=5`, `Security:Alerts:RefreshReuseCount=1`, `Security:Alerts:NewDeviceLoginCount=3`, `Security:Alerts:WindowMinutes=10`. Threshold values are clamped to safe positive ranges. Alerts include safe account type/ID, optional session ID, count/window and CorrelationId only. Tune thresholds to environment and monitor for false positives; do not place credentials or request bodies in warning messages.
