namespace Mytril.Audit.Sdk;

public sealed record AuditableEvent
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public string EventVersion { get; init; } = "1.0";
    public required string Source { get; init; }
    public AuditSeverity Severity { get; init; } = AuditSeverity.Info;
    public Guid? UserId { get; init; }
    public Guid? ActorId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? TenantProductId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public required string TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public required string Payload { get; init; }
    public required DateTime OccurredAt { get; init; }
}
