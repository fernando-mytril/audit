using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Domain.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public EventTypeLabel EventType { get; private set; } = default!;
    public string EventVersion { get; private set; } = default!;
    public EventSource Source { get; private set; } = default!;
    public AuditSeverity Severity { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ActorId { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? TenantProductId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string TraceId { get; private set; } = default!;
    public string? CorrelationId { get; private set; }
    public SanitizedPayload Payload { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    private AuditEntry() { }

    public static AuditEntry Create(
        Guid eventId,
        string eventType,
        string eventVersion,
        string source,
        AuditSeverity severity,
        string traceId,
        string payload,
        DateTime occurredAt,
        Guid? userId = null,
        Guid? actorId = null,
        Guid? tenantId = null,
        Guid? tenantProductId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null)
    {
        if (eventId == Guid.Empty)
            throw new InvalidAuditEnvelopeException("EventId cannot be empty.");

        if (string.IsNullOrWhiteSpace(traceId))
            throw new InvalidAuditEnvelopeException("TraceId cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidAuditEnvelopeException("Source cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(eventType))
            throw new InvalidAuditEnvelopeException("EventType cannot be null or empty.");

        if (occurredAt == default)
            throw new InvalidAuditEnvelopeException("OccurredAt must be a valid date.");

        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = new EventTypeLabel(eventType),
            EventVersion = eventVersion,
            Source = new EventSource(source),
            Severity = severity,
            UserId = userId,
            ActorId = actorId,
            TenantId = tenantId,
            TenantProductId = tenantProductId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TraceId = traceId,
            CorrelationId = correlationId,
            Payload = new SanitizedPayload(payload),
            OccurredAt = occurredAt,
            ReceivedAt = DateTime.UtcNow
        };
    }
}
