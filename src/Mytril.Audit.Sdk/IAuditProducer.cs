namespace Mytril.Audit.Sdk;

public interface IAuditProducer
{
    Task PublishAsync(
        string eventType, string payload,
        AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null,
        Guid? tenantId = null, Guid? tenantProductId = null,
        string? ipAddress = null, string? correlationId = null,
        string? traceId = null, CancellationToken ct = default);

    Task PublishAsync<TPayload>(
        string eventType, TPayload payload,
        AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null,
        Guid? tenantId = null, Guid? tenantProductId = null,
        string? ipAddress = null, string? correlationId = null,
        string? traceId = null, CancellationToken ct = default);
}
