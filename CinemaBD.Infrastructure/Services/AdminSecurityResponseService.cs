using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class AdminSecurityResponseService : IAdminSecurityResponseService
{
    private readonly AppDbContext _db;
    private readonly IIdentitySessionService _sessions;
    private readonly IAdministrativeAccountLockoutService _lockout;
    private readonly ISecurityAuditWriter _audit;

    public AdminSecurityResponseService(AppDbContext db, IIdentitySessionService sessions, IAdministrativeAccountLockoutService lockout, ISecurityAuditWriter audit)
    {
        _db = db;
        _sessions = sessions;
        _lockout = lockout;
        _audit = audit;
    }

    public async Task<bool> RevokeAllSessionsAsync(string accountType, string accountId, CancellationToken cancellationToken = default)
    {
        if (!await AccountExistsAsync(accountType, accountId, cancellationToken)) return false;
        await _sessions.RevokeAllAsync(CanonicalAccountType(accountType), accountId, "admin_response", cancellationToken);
        await WriteAuditAsync("admin_sessions_revoked", accountType, accountId, "All identity sessions and refresh credentials revoked.", cancellationToken);
        return true;
    }

    public async Task<bool> SetAccountLockAsync(string accountType, string accountId, bool locked, CancellationToken cancellationToken = default)
    {
        if (!await AccountExistsAsync(accountType, accountId, cancellationToken)) return false;
        if (locked && await IsLastUsableMasterAsync(accountType, accountId, cancellationToken))
            throw new InvalidOperationException("Không thể khóa tài khoản Master cuối cùng đang hoạt động.");
        if (!await _lockout.SetAdministrativeLockAsync(CanonicalAccountType(accountType), accountId, locked, cancellationToken))
            return false;
        if (locked)
            await _sessions.RevokeAllAsync(CanonicalAccountType(accountType), accountId, "admin_account_locked", cancellationToken);
        await WriteAuditAsync(locked ? "admin_account_locked" : "admin_account_unlocked", accountType, accountId,
            locked ? "Administrative account lock applied." : "Administrative account lock removed.", cancellationToken);
        return true;
    }

    public async Task<bool> ForcePasswordChangeAsync(string accountType, string accountId, CancellationToken cancellationToken = default)
    {
        if (IsAdmin(accountType) && int.TryParse(accountId, out var adminId))
        {
            if (!_db.Database.IsRelational())
            {
                var admin = await _db.Admins.SingleOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);
                if (admin is null) return false;
                admin.MustChangePassword = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (await _db.Admins.Where(x => x.AdminID == adminId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.MustChangePassword, true), cancellationToken) == 0)
                return false;
        }
        else if (IsCustomer(accountType))
        {
            if (!_db.Database.IsRelational())
            {
                var customer = await _db.Customers.SingleOrDefaultAsync(x => x.MaKH == accountId, cancellationToken);
                if (customer is null) return false;
                customer.MustChangePassword = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (await _db.Customers.Where(x => x.MaKH == accountId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.MustChangePassword, true), cancellationToken) == 0)
                return false;
        }
        else return false;

        await _sessions.RevokeAllAsync(CanonicalAccountType(accountType), accountId, "forced_password_change", cancellationToken);
        await WriteAuditAsync("admin_password_change_required", accountType, accountId, "Password change required; all identity sessions revoked.", cancellationToken);
        return true;
    }

    private async Task<bool> AccountExistsAsync(string accountType, string accountId, CancellationToken cancellationToken)
    {
        if (IsAdmin(accountType) && int.TryParse(accountId, out var adminId))
            return await _db.Admins.AnyAsync(x => x.AdminID == adminId, cancellationToken);
        if (IsCustomer(accountType))
            return await _db.Customers.AnyAsync(x => x.MaKH == accountId, cancellationToken);
        return false;
    }

    private async Task<bool> IsLastUsableMasterAsync(string accountType, string accountId, CancellationToken cancellationToken)
    {
        if (!IsAdmin(accountType) || !int.TryParse(accountId, out var adminId)) return false;
        var admin = await _db.Admins.SingleOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);
        if (admin?.MaCV is not > 0) return false;
        if (!await _db.Roles.AnyAsync(x => x.MaCV == admin.MaCV && x.IsActive && x.IsMaster, cancellationToken)) return false;
        if (!_db.Database.IsRelational())
        {
            var activeMasterIds = await (from candidate in _db.Admins
                                         join role in _db.Roles on candidate.MaCV equals role.MaCV
                                         where candidate.IsActive && role.IsActive && role.IsMaster
                                               && (candidate.LockoutEndUtc == null || candidate.LockoutEndUtc <= DateTime.UtcNow)
                                         select candidate.AdminID).ToListAsync(cancellationToken);
            return activeMasterIds.Count <= 1 && activeMasterIds.Contains(adminId);
        }

        var otherUsableMasters = await (from candidate in _db.Admins
                                        join role in _db.Roles on candidate.MaCV equals role.MaCV
                                        where candidate.IsActive && role.IsActive && role.IsMaster
                                              && candidate.AdminID != adminId
                                              && (candidate.LockoutEndUtc == null || candidate.LockoutEndUtc <= DateTime.UtcNow)
                                        select candidate.AdminID).CountAsync(cancellationToken);
        return otherUsableMasters == 0;
    }

    private Task WriteAuditAsync(string eventType, string accountType, string accountId, string details, CancellationToken cancellationToken)
        => _audit.WriteAsync(new SecurityAuditEvent(eventType, CanonicalAccountType(accountType), accountId, Result: "success", Details: details), cancellationToken);

    private static string CanonicalAccountType(string accountType) => IsAdmin(accountType) ? "Admin" : "KHACHHANG";
    private static bool IsAdmin(string accountType) => string.Equals(accountType, "admin", StringComparison.OrdinalIgnoreCase);
    private static bool IsCustomer(string accountType) => string.Equals(accountType, "KHACHHANG", StringComparison.OrdinalIgnoreCase)
        || string.Equals(accountType, "customer", StringComparison.OrdinalIgnoreCase);
}
