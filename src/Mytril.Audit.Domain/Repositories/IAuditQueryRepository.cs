using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Enums;

namespace Mytril.Audit.Domain.Repositories;

public interface IAuditQueryRepository
{
    Task<(List<AuditEntry> Items, int TotalCount)> FindByTraceIdAsync(
        string traceId,
        CancellationToken ct = default);

    Task<(List<AuditEntry> Items, int TotalCount)> FindBySourceAsync(
        string source,
        DateTime? from,
        DateTime? to,
        AuditSeverity? severity,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(List<AuditEntry> Items, int TotalCount)> FindByActorAsync(
        Guid actorId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(List<AuditEntry> Items, int TotalCount)> FindByUserAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(List<AuditEntry> Items, int TotalCount)> FindByTenantAsync(
        Guid tenantId,
        string? source,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(List<AuditEntry> Items, int TotalCount)> FindByFilterAsync(
        string? source,
        string? eventType,
        AuditSeverity? severity,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
