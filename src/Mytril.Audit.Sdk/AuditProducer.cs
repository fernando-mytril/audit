using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mytril.Audit.Sdk;

public sealed class AuditProducer : IAuditProducer
{
    private readonly AuditProducerOptions _options;
    private readonly IEventOutbox _outbox;

    public AuditProducer(IOptions<AuditProducerOptions> options, IEventOutbox outbox)
    {
        _options = options.Value;
        _outbox = outbox;
    }

    public async Task PublishAsync(
        string eventType, string payload,
        AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null,
        Guid? tenantId = null, Guid? tenantProductId = null,
        string? ipAddress = null, string? correlationId = null,
        string? traceId = null, CancellationToken ct = default)
    {
        var resolvedTraceId = traceId ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        var auditEvent = new AuditableEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Source = _options.Source,
            Severity = severity,
            UserId = userId,
            ActorId = actorId,
            TenantId = tenantId,
            TenantProductId = tenantProductId,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            TraceId = resolvedTraceId,
            Payload = payload,
            OccurredAt = DateTime.UtcNow
        };

        var body = JsonSerializer.Serialize(auditEvent);
        var routingKey = $"{_options.RoutingKeyPrefix}.{_options.Source}.{eventType}";

        await _outbox.PublishAsync("audit", routingKey, body, ct);
    }

    public Task PublishAsync<TPayload>(
        string eventType, TPayload payload,
        AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null,
        Guid? tenantId = null, Guid? tenantProductId = null,
        string? ipAddress = null, string? correlationId = null,
        string? traceId = null, CancellationToken ct = default)
    {
        var serializedPayload = JsonSerializer.Serialize(payload);

        return PublishAsync(
            eventType, serializedPayload,
            severity,
            userId, actorId,
            tenantId, tenantProductId,
            ipAddress, correlationId,
            traceId, ct);
    }
}
