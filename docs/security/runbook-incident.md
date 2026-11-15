# Incident Response and Recovery Runbook

## Purpose and boundaries

Use this runbook for suspected account compromise, credential theft, MFA device loss, Data Protection key loss, or database recovery. Preserve evidence and obtain the incident lead's authorization before destructive changes. The `ADMIN_RECOVERY_KEY` path is an out-of-band operator procedure, not an HTTP endpoint. It is not a replacement for MFA or routine account recovery.

## 1. Initial triage and evidence preservation

1. Assign an incident lead and record incident ID, time zone, systems, affected accounts, and operator actions.
2. Preserve API/Web logs, deployment/configuration history, SecurityAudit records, and relevant database backups as read-only evidence. Record hashes and access custody using the organization's evidence procedure.
3. Check alert/event counts by account, session, time window, and event type through the authorized security-audit query. Export only the minimum necessary, retain original timestamps/CorrelationIds, and do not export tokens, passwords, MFA seeds, recovery codes, or key material.
4. Establish whether active investigation/legal hold applies. If so, set `Security:Cleanup:LegalHold=true` and confirm the cleanup job skips deletion; retain evidence until release is approved.

## 2. Emergency containment: account, sessions, and tokens

1. From an authorized, MFA-verified administrator with `phanquyen` or Master access, use account response controls to lock the affected Admin/KHACHHANG identity. The last usable Master cannot be locked; for an emergency affecting that account, use the break-glass procedure below and ensure another Master/operator is available.
2. Revoke every identity session and refresh credential for affected accounts. If compromise may extend to administrator credentials, revoke all targeted admin sessions, not only the operator's current session.
3. Force password change for the affected account. This also revokes sessions. For an Admin, confirm access restrictions allow only the password-change endpoint until the user changes the password.
4. If a JWT signing key or refresh-token compromise is suspected, escalate to the service owner for key rotation/revocation and client reauthentication; do not put secrets in the incident log.
5. Record actor, target account type/ID, time, reason, and resulting audit IDs/CorrelationIds. Verify containment by observing affected sessions become inactive and refresh requests are rejected.

## 3. Lost or compromised MFA device

1. Verify identity using the organization's independent identity proofing process; do not accept proof from the lost device or the potentially compromised account session alone.
2. If a valid single-use recovery code remains, authenticate through the existing MFA challenge flow and rotate/remove the lost factor through the supported admin MFA process. Do not record the code in tickets or logs.
3. If recovery codes are unavailable, use the break-glass `ADMIN_RECOVERY_KEY` procedure only on the trusted operator host (Section 5). The recovery operation resets MFA state and requires a password change; it does not expose an HTTP route.
4. Re-enroll MFA on a newly controlled device, generate and securely store new recovery codes, verify one successful login, then revoke sessions that predate recovery and review recent audit history.
5. Treat unexplained recovery-code use or enrollment changes as possible compromise and expand scope to related sessions and devices.

## 4. Data Protection key loss

1. Stop changing protected MFA material and preserve the deployed key-ring directory, deployment logs, backups, and host/container identity. Do not delete key-ring files as a troubleshooting shortcut.
2. Determine whether the original key ring can be restored from a protected backup to the same application name and authorized deployment. Restore keys only through the approved secrets/backup process; validate file permissions and backups before restarting the API.
3. If the original key is permanently unavailable, existing protected MFA seeds cannot be assumed decryptable. Keep affected admins contained, verify identities independently, use the authorized break-glass recovery for each impacted Master if required, then re-enroll MFA and issue new recovery codes.
4. Check all API instances share the intended durable key ring and application name; perform a staged test before returning all instances to service.
5. Never log, commit, paste into an incident ticket, or include Data Protection key material in audit details.

## 5. Master break-glass recovery with `ADMIN_RECOVERY_KEY`

1. Obtain two-person incident approval and use the documented secure operator environment. Verify the recovery key from the designated secret manager; do not copy it into commands, shell history, source files, or logs.
2. Run the existing non-HTTP `IAdminEmergencyRecoveryService` operation using the approved operator workflow. The operation is protected by `ADMIN_RECOVERY_KEY` (or supported `Security:AdminRecoveryKey` configuration), resets MFA enrollment/recovery records, forces a password change, and revokes sessions.
3. Confirm the target is the intended Master by independently checking its immutable admin ID and role before execution. Never use this path to bypass ordinary authorization or to weaken controls for convenience.
4. Confirm the service reports success; inspect safe audit metadata without recording the secret or recovery payload. If the operation fails or state is ambiguous, keep the account contained and escalate rather than retrying blindly.
5. Remove temporary secret access and rotate the recovery key according to policy after the incident. Validate that another authorized Master is operational and re-enroll MFA for the recovered account.

## 6. Database restore and integrity validation

1. Obtain change approval, identify the exact database/environment, select a known-good encrypted backup and point-in-time target, and verify backup integrity using the SQL Server backup verification procedure. Record the recovery point and expected data-loss window.
2. Restore to an isolated verification database first; do not overwrite production as a diagnostic step. Verify schema/migration history, account/role/permission integrity, `IdentitySessions`, `RefreshTokens`, `SecurityAudit`, MFA/recovery tables, and the B6 lockout columns.
3. Validate referential integrity, representative booking/payment data, and application authentication checks in the isolated environment. Apply only approved, reviewed migrations; preserve the current retention/legal hold requirements.
4. Obtain incident-lead and data-owner approval before promoting a restore. Capture a fresh backup before cutover, restrict write access during cutover, and monitor audit and business reconciliation after service resumes.
5. Confirm session/token invalidation policy after restore. Revoke sessions created after the recovery point or require global reauthentication when the recovered state cannot establish reliable credential freshness.

## 7. Recovery and post-incident checklist

- [ ] Containment is verified; compromised accounts are unlocked only after identity verification and credential reset.
- [ ] Admin MFA was re-enrolled and recovery codes safely transferred to the approved vault.
- [ ] Data Protection/JWT/recovery secrets are available only to authorized deployment identities and have been rotated where indicated.
- [ ] Database restore and application data were verified in isolation before promotion; backup integrity evidence is retained.
- [ ] Cleanup legal hold was released only with approval; retention policy is restored and the job's outcome is monitored.
- [ ] SecurityAudit records and service logs correlate through safe CorrelationIds; no secrets were introduced into logs or incident notes.
- [ ] API/application instances, clients, and operators were returned to service using the approved deployment process.
- [ ] Update `incident-log.md` with confirmed timeline, impact, root cause, containment/recovery evidence, unresolved questions, and owners. Do not invent timestamps or attribute causality beyond evidence.
- [ ] Capture a clean clone/reproducible build and focused test results after the fix. Record any required SQL Server-only validation as pending until actually run.
