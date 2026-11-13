using System.Collections.Concurrent;
using CinemaBD.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Infrastructure.Services;

/// <summary>In-memory lockout policy state; durable account columns are proposed in a separate, unapplied migration.</summary>
public sealed class AccountLockoutService : IAccountLockoutService
{
    private readonly ConcurrentDictionary<string, State> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _threshold;
    private readonly TimeSpan _duration;
    private readonly TimeProvider _timeProvider;

    public AccountLockoutService(IConfiguration configuration, TimeProvider? timeProvider = null)
    {
        _threshold = Math.Clamp(configuration.GetValue("Security:AccountLockout:MaxFailedAttempts", 5), 1, 100);
        _duration = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Security:AccountLockout:LockoutMinutes", 15), 1, 1440));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<AccountLockoutSnapshot> GetAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(Key(accountType, username), out var state);
        return Task.FromResult(Snapshot(state, null));
    }

    public Task<AccountLockoutSnapshot> RecordFailureAsync(string accountType, string username, string? accountId, CancellationToken cancellationToken = default)
    {
        var key = Key(accountType, username);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var state = _states.AddOrUpdate(key,
            _ => new State(accountId, 1, _threshold == 1 ? now.Add(_duration) : null),
            (_, current) =>
            {
                if (current.LockoutEndUtc is { } end && end > now)
                    return current with { AccountId = accountId ?? current.AccountId };
                var count = current.LockoutEndUtc is { } expired && expired <= now ? 1 : current.FailedAttempts + 1;
                return new State(accountId ?? current.AccountId, count, count == _threshold ? now.Add(_duration) : null);
            });
        return Task.FromResult(Snapshot(state, accountId));
    }

    public Task ResetAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(Key(accountType, username), out _);
        return Task.CompletedTask;
    }

    public Task<AccountLockoutSnapshot?> UnlockAsync(string accountType, string username, CancellationToken cancellationToken = default)
    {
        if (!_states.TryRemove(Key(accountType, username), out var state) || state.LockoutEndUtc is null)
            return Task.FromResult<AccountLockoutSnapshot?>(null);
        return Task.FromResult<AccountLockoutSnapshot?>(new AccountLockoutSnapshot(state.AccountId, false, null, 0));
    }

    private static string Key(string accountType, string username) => $"{accountType.Trim()}:{username.Trim()}";

    private AccountLockoutSnapshot Snapshot(State? state, string? accountId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (state is null) return new AccountLockoutSnapshot(accountId, false, null, 0);
        if (state.LockoutEndUtc is { } end && end <= now)
            return new AccountLockoutSnapshot(state.AccountId ?? accountId, false, end, 0);
        return new AccountLockoutSnapshot(state.AccountId ?? accountId, state.LockoutEndUtc is { } active && active > now, state.LockoutEndUtc, state.FailedAttempts);
    }

    private sealed record State(string? AccountId, int FailedAttempts, DateTime? LockoutEndUtc);
}
