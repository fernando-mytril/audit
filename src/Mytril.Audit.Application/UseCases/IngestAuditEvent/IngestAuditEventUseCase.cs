using Microsoft.Extensions.Logging;
using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.UseCases.IngestAuditEvent;

public sealed class IngestAuditEventUseCase(
    IAuditRepository repository,
    ILogger<IngestAuditEventUseCase> logger)
{
    public async Task ExecuteAsync(IngestAuditEventCommand command, CancellationToken ct = default)
    {
        var entry = AuditEntry.Create(
            command.EventId,
            command.EventType,
            command.EventVersion,
            command.Source,
            command.Severity,
            command.TraceId,
            command.Payload,
            command.OccurredAt,
            command.UserId,
            command.ActorId,
            command.TenantId,
            command.TenantProductId,
            command.IpAddress,
            command.UserAgent,
            command.CorrelationId);

        try
        {
            await repository.InsertAsync(entry, ct);
        }
        catch (Exception ex) when (
            ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "Duplicate audit event detected at DB level for EventId {EventId}",
                command.EventId);

            throw new DuplicateAuditEventException(command.EventId);
        }

        logger.LogInformation(
            "Audit event ingested — EventId: {EventId}, EventType: {EventType}, Source: {Source}",
            command.EventId, command.EventType, command.Source);
    }
}
