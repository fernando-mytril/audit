using Mytril.Audit.Domain.Services;
using StackExchange.Redis;

namespace Mytril.Audit.Infrastructure.Idempotency;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer redis) : IIdempotencyStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private const string KeyPrefix = "audit:idempotent:";

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        return await db.KeyExistsAsync(KeyPrefix + eventId);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(KeyPrefix + eventId, "1", Ttl);
    }
}
