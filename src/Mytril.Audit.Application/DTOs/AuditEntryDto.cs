using Mytril.Audit.Domain.Entities;

namespace Mytril.Audit.Application.DTOs;

public sealed record AuditEntryDto(
    Guid Id,
    Guid EventId,
    string EventType,
    string EventVersion,
    string Source,
    string Severity,
    Guid? UserId,
    Guid? ActorId,
    Guid? TenantId,
    Guid? TenantProductId,
    string? IpAddress,
    string? UserAgent,
    string TraceId,
    string? CorrelationId,
    string Payload,
    DateTime OccurredAt,
    DateTime ReceivedAt)
{
    public static AuditEntryDto From(AuditEntry entry) => new(
        entry.Id,
        entry.EventId,
        entry.EventType.Value,
        entry.EventVersion,
        entry.Source.Value,
        entry.Severity.ToString(),
        entry.UserId,
        entry.ActorId,
        entry.TenantId,
        entry.TenantProductId,
        entry.IpAddress,
        entry.UserAgent,
        entry.TraceId,
        entry.CorrelationId,
        entry.Payload.Value,
        entry.OccurredAt,
        entry.ReceivedAt);
}
