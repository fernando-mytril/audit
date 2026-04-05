using Mytril.Audit.Domain.Enums;

namespace Mytril.Audit.Application.UseCases.IngestAuditEvent;

public sealed record IngestAuditEventCommand(
    Guid EventId,
    string EventType,
    string EventVersion,
    string Source,
    AuditSeverity Severity,
    string TraceId,
    string Payload,
    DateTime OccurredAt,
    Guid? UserId = null,
    Guid? ActorId = null,
    Guid? TenantId = null,
    Guid? TenantProductId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null);
