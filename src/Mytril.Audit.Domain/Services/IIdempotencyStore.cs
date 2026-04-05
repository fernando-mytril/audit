namespace Mytril.Audit.Domain.Services;

public interface IIdempotencyStore
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, CancellationToken ct = default);
}
