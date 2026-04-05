using System.Collections.Concurrent;
using Mytril.Audit.Domain.Services;

namespace Mytril.Audit.Infrastructure.Idempotency;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private const int MaxEntries = 50_000;

    private readonly ConcurrentDictionary<Guid, DateTime> _store = new();

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
    {
        if (_store.TryGetValue(eventId, out var timestamp))
        {
            if (DateTime.UtcNow - timestamp < Ttl)
                return Task.FromResult(true);

            _store.TryRemove(eventId, out _);
        }

        return Task.FromResult(false);
    }

    public Task MarkProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        if (_store.Count >= MaxEntries)
            Cleanup();

        _store[eventId] = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow - Ttl;
        var expired = _store
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expired)
            _store.TryRemove(key, out _);
    }
}
