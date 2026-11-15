using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using CinemaBD.Application.Interfaces;

namespace CinemaBD.Infrastructure.Services;

/// <summary>Persists consecutive login failure state on Admin and KHACHHANG records.</summary>
public sealed class AccountLockoutService : IAccountLockoutService
{
    private readonly AppDbContext _db;
    private readonly int _threshold;
    private readonly TimeSpan _duration;
    private readonly TimeProvider _timeProvider;

    public AccountLockoutService(AppDbContext db, IConfiguration configuration, TimeProvider? timeProvider = null)
    {
        _db = db;
        _threshold = Math.Clamp(configuration.GetValue("Security:AccountLockout:MaxFailedAttempts", 5), 1, 100);
        _duration = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Security:AccountLockout:LockoutMinutes", 15), 1, 1440));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AccountLockoutSnapshot> GetAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        var state = await FindStateAsync(accountType, NormalizeUsername(username), cancellationToken);
        if (state is null)
            return new AccountLockoutSnapshot(null, false, null, 0);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var isLocked = state.LockoutEndUtc is { } end && end > now;
        return new AccountLockoutSnapshot(state.AccountId, isLocked, state.LockoutEndUtc, state.FailedAttempts);
    }

    public async Task<AccountLockoutSnapshot> RecordFailureAsync(string accountType, string username, string? accountId, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var newLockoutEnd = now.Add(_duration);
        if (_db.Database.IsRelational())
            await ApplyRelationalFailureAsync(accountType, normalizedUsername, now, newLockoutEnd, cancellationToken);
        else
            await ApplyTrackedFailureAsync(accountType, normalizedUsername, now, newLockoutEnd, accountId, cancellationToken);

        // Unknown accounts have no row on which to persist state. They use the existing
        // auth-account/IP rate limit; don't keep attacker-controlled usernames in memory here.
        var state = await FindStateAsync(accountType, normalizedUsername, cancellationToken);
        if (state is null)
            return new AccountLockoutSnapshot(accountId, false, null, 0);

        // A locked row is not incremented further while the lock is active. The returned state
        // is read after the atomic relational update, so it reflects the persisted counter.
        var isLocked = state.LockoutEndUtc is { } end && end > now;
        return new AccountLockoutSnapshot(state.AccountId, isLocked, state.LockoutEndUtc, state.FailedAttempts);
    }

    public async Task ResetAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (!_db.Database.IsRelational())
        {
            if (IsAdmin(accountType))
            {
                var admin = await _db.Admins.FirstOrDefaultAsync(x => x.Username.ToUpper() == normalizedUsername, cancellationToken);
                if (admin is null) return;
                admin.AccessFailedCount = null;
                admin.LockoutEndUtc = null;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            if (IsCustomer(accountType))
            {
                var customer = await _db.Customers.FirstOrDefaultAsync(x => x.TKhoan.ToUpper() == normalizedUsername, cancellationToken);
                if (customer is null) return;
                customer.AccessFailedCount = null;
                customer.LockoutEndUtc = null;
                await _db.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        if (IsAdmin(accountType))
        {
            await _db.Admins.Where(x => x.Username.ToUpper() == normalizedUsername)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount, (int?)null)
                    .SetProperty(x => x.LockoutEndUtc, (DateTime?)null), cancellationToken);
            return;
        }

        if (IsCustomer(accountType))
        {
            await _db.Customers.Where(x => x.TKhoan.ToUpper() == normalizedUsername)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount, (int?)null)
                    .SetProperty(x => x.LockoutEndUtc, (DateTime?)null), cancellationToken);
        }
    }

    public async Task<AccountLockoutSnapshot?> UnlockAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        var before = await FindStateAsync(accountType, NormalizeUsername(username), cancellationToken);
        if (before is null || before.LockoutEndUtc is null)
            return null;
        await ResetAsync(accountType, username, cancellationToken);
        return new AccountLockoutSnapshot(before.AccountId, false, null, 0);
    }

    /// <summary>Sets or clears an explicit administrative lockout using the persisted columns.</summary>
    public async Task<bool> SetAdministrativeLockAsync(string accountType, string accountId, bool locked, CancellationToken cancellationToken = default)
    {
        var manualLockUntil = locked ? DateTime.MaxValue : (DateTime?)null;
        if (IsAdmin(accountType) && int.TryParse(accountId, out var adminId))
        {
            if (!_db.Database.IsRelational())
            {
                var admin = await _db.Admins.FindAsync([adminId], cancellationToken);
                if (admin is null) return false;
                admin.AccessFailedCount = locked ? _threshold : null;
                admin.LockoutEndUtc = manualLockUntil;
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            var count = await _db.Admins.Where(x => x.AdminID == adminId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount, locked ? _threshold : (int?)null)
                    .SetProperty(x => x.LockoutEndUtc, manualLockUntil), cancellationToken);
            return count > 0;
        }

        if (IsCustomer(accountType))
        {
            if (!_db.Database.IsRelational())
            {
                var customer = await _db.Customers.FindAsync([accountId], cancellationToken);
                if (customer is null) return false;
                customer.AccessFailedCount = locked ? _threshold : null;
                customer.LockoutEndUtc = manualLockUntil;
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            var count = await _db.Customers.Where(x => x.MaKH == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount, locked ? _threshold : (int?)null)
                    .SetProperty(x => x.LockoutEndUtc, manualLockUntil), cancellationToken);
            return count > 0;
        }
        return false;
    }

    private async Task<int> ApplyRelationalFailureAsync(string accountType, string username, DateTime now, DateTime lockoutEnd, CancellationToken cancellationToken)
    {
        if (IsAdmin(accountType))
        {
            return await _db.Admins
                .Where(x => x.Username.ToUpper() == username && (x.LockoutEndUtc == null || x.LockoutEndUtc <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount,
                        x => x.LockoutEndUtc != null && x.LockoutEndUtc <= now ? 1 : (x.AccessFailedCount ?? 0) + 1)
                    .SetProperty(x => x.LockoutEndUtc,
                        x => (x.LockoutEndUtc != null && x.LockoutEndUtc <= now ? 1 : (x.AccessFailedCount ?? 0) + 1) >= _threshold
                            ? lockoutEnd
                            : (DateTime?)null), cancellationToken);
        }

        if (IsCustomer(accountType))
        {
            return await _db.Customers
                .Where(x => x.TKhoan.ToUpper() == username && (x.LockoutEndUtc == null || x.LockoutEndUtc <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AccessFailedCount,
                        x => x.LockoutEndUtc != null && x.LockoutEndUtc <= now ? 1 : (x.AccessFailedCount ?? 0) + 1)
                    .SetProperty(x => x.LockoutEndUtc,
                        x => (x.LockoutEndUtc != null && x.LockoutEndUtc <= now ? 1 : (x.AccessFailedCount ?? 0) + 1) >= _threshold
                            ? lockoutEnd
                            : (DateTime?)null), cancellationToken);
        }
        return 0;
    }

    private async Task<int> ApplyTrackedFailureAsync(string accountType, string username, DateTime now, DateTime lockoutEnd, string? accountId, CancellationToken cancellationToken)
    {
        if (IsAdmin(accountType))
        {
            var entity = int.TryParse(accountId, out var adminId)
                ? await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken)
                : await _db.Admins.FirstOrDefaultAsync(x => x.Username.ToUpper() == username, cancellationToken);
            if (entity is null || entity.LockoutEndUtc > now) return 0;
            var attempts = entity.LockoutEndUtc is not null ? 1 : (entity.AccessFailedCount ?? 0) + 1;
            entity.AccessFailedCount = attempts;
            entity.LockoutEndUtc = attempts >= _threshold ? lockoutEnd : null;
            await _db.SaveChangesAsync(cancellationToken);
            return 1;
        }

        if (IsCustomer(accountType))
        {
            var entity = !string.IsNullOrWhiteSpace(accountId)
                ? await _db.Customers.FirstOrDefaultAsync(x => x.MaKH == accountId, cancellationToken)
                : await _db.Customers.FirstOrDefaultAsync(x => x.TKhoan.ToUpper() == username, cancellationToken);
            if (entity is null || entity.LockoutEndUtc > now) return 0;
            var attempts = entity.LockoutEndUtc is not null ? 1 : (entity.AccessFailedCount ?? 0) + 1;
            entity.AccessFailedCount = attempts;
            entity.LockoutEndUtc = attempts >= _threshold ? lockoutEnd : null;
            await _db.SaveChangesAsync(cancellationToken);
            return 1;
        }
        return 0;
    }

    private async Task<AccountState?> FindStateAsync(string accountType, string username, CancellationToken cancellationToken)
    {
        if (IsAdmin(accountType))
        {
            return await _db.Admins.AsNoTracking()
                .Where(x => x.Username.ToUpper() == username)
                .Select(x => new AccountState(x.AdminID.ToString(), x.AccessFailedCount ?? 0, x.LockoutEndUtc))
                .SingleOrDefaultAsync(cancellationToken);
        }
        if (IsCustomer(accountType))
        {
            return await _db.Customers.AsNoTracking()
                .Where(x => x.TKhoan.ToUpper() == username)
                .Select(x => new AccountState(x.MaKH, x.AccessFailedCount ?? 0, x.LockoutEndUtc))
                .SingleOrDefaultAsync(cancellationToken);
        }
        return null;
    }

    private static string NormalizeUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return username.Trim().ToUpperInvariant();
    }

    private static bool IsAdmin(string accountType) => string.Equals(accountType, "Admin", StringComparison.OrdinalIgnoreCase);
    private static bool IsCustomer(string accountType) => string.Equals(accountType, "KHACHHANG", StringComparison.OrdinalIgnoreCase);
    private sealed record AccountState(string AccountId, int FailedAttempts, DateTime? LockoutEndUtc);
}
