using Microsoft.EntityFrameworkCore;
using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Repositories;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Infrastructure.Persistence.Repositories;

public sealed class AuditQueryRepository(AuditDbContext dbContext) : IAuditQueryRepository
{
    public async Task<(List<AuditEntry> Items, int TotalCount)> FindByTraceIdAsync(
        string traceId,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditLog
            .AsNoTracking()
            .Where(e => e.TraceId == traceId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(List<AuditEntry> Items, int TotalCount)> FindBySourceAsync(
        string source,
        DateTime? from,
        DateTime? to,
        AuditSeverity? severity,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sourceVo = new EventSource(source);

        var query = dbContext.AuditLog
            .AsNoTracking()
            .Where(e => e.Source == sourceVo);

        query = ApplyDateRange(query, from, to);

        if (severity.HasValue)
            query = query.Where(e => e.Severity == severity.Value);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<(List<AuditEntry> Items, int TotalCount)> FindByActorAsync(
        Guid actorId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditLog
            .AsNoTracking()
            .Where(e => e.ActorId == actorId);

        query = ApplyDateRange(query, from, to);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<(List<AuditEntry> Items, int TotalCount)> FindByUserAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditLog
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        query = ApplyDateRange(query, from, to);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<(List<AuditEntry> Items, int TotalCount)> FindByTenantAsync(
        Guid tenantId,
        string? source,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditLog
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceVo = new EventSource(source);
            query = query.Where(e => e.Source == sourceVo);
        }

        query = ApplyDateRange(query, from, to);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<(List<AuditEntry> Items, int TotalCount)> FindByFilterAsync(
        string? source,
        string? eventType,
        AuditSeverity? severity,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditLog.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceVo = new EventSource(source);
            query = query.Where(e => e.Source == sourceVo);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var eventTypeVo = new EventTypeLabel(eventType);
            query = query.Where(e => e.EventType == eventTypeVo);
        }

        if (severity.HasValue)
            query = query.Where(e => e.Severity == severity.Value);

        query = ApplyDateRange(query, from, to);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    private static IQueryable<AuditEntry> ApplyDateRange(
        IQueryable<AuditEntry> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        return query;
    }

    private static async Task<(List<AuditEntry> Items, int TotalCount)> PaginateAsync(
        IQueryable<AuditEntry> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
