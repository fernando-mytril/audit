using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mytril.Audit.Sdk.Internal;

namespace Mytril.Audit.Sdk;

public sealed class AuditProducer(
    IOutboxRepository outboxRepository,
    IOptions<AuditProducerOptions> opts,
    ILogger<AuditProducer> logger
) : IAuditProducer
{
    private static readonly JsonSerializerOptions CamelCaseOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AuditProducerOptions _opts = opts.Value;

    public Task PublishAsync<TPayload>(
        string eventType, TPayload payload, AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null, Guid? tenantId = null,
        Guid? tenantProductId = null, string? ipAddress = null,
        string? correlationId = null, string? traceId = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, CamelCaseOptions);

        return PublishAsync(eventType, json, severity, userId, actorId, tenantId,
            tenantProductId, ipAddress, correlationId, traceId, ct);
    }

    public async Task PublishAsync(
        string eventType, string payload, AuditSeverity severity = AuditSeverity.Info,
        Guid? userId = null, Guid? actorId = null, Guid? tenantId = null,
        Guid? tenantProductId = null, string? ipAddress = null,
        string? correlationId = null, string? traceId = null, CancellationToken ct = default)
    {
        var (sanitizedPayload, removedFields) = PayloadValidator.SanitizeAndReport(payload);

        if (removedFields.Count > 0)
        {
            logger.LogWarning(
                "Forbidden fields removed from audit payload | source={Source} eventType={EventType} fields={Fields}",
                _opts.Source, eventType, string.Join(", ", removedFields));
        }

        var envelope = new AuditableEvent
        {
            EventType       = eventType.ToUpperInvariant(),
            Source          = _opts.Source,
            Severity        = severity,
            TraceId         = traceId ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
            CorrelationId   = correlationId,
            Payload         = sanitizedPayload,
            UserId          = userId,
            ActorId         = actorId,
            TenantId        = tenantId,
            TenantProductId = tenantProductId,
            IpAddress       = ipAddress,
        };

        await outboxRepository.AddAuditEventAsync(envelope, ct);

        logger.LogDebug(
            "Audit event queued | eventType={EventType} eventId={EventId} source={Source} traceId={TraceId}",
            envelope.EventType, envelope.EventId, _opts.Source, envelope.TraceId);
    }
}
