using Microsoft.Extensions.Logging;
using Mytril.Audit.Application.UseCases.IngestAuditEvent;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.Services;

namespace Mytril.Audit.Application.Consumers;

public sealed class AuditEventConsumer(
    IngestAuditEventUseCase ingestUseCase,
    IIdempotencyStore idempotencyStore,
    IPayloadSanitizer payloadSanitizer,
    ILogger<AuditEventConsumer> logger)
{
    public async Task ConsumeAsync(IngestAuditEventCommand command, CancellationToken ct = default)
    {
        ValidateRequired(command);

        if (await idempotencyStore.ExistsAsync(command.EventId, ct))
        {
            logger.LogDebug(
                "Audit event {EventId} already processed — skipping",
                command.EventId);
            return;
        }

        if (payloadSanitizer.HasForbiddenFields(command.Payload))
        {
            logger.LogWarning(
                "Audit event {EventId} contains forbidden fields in payload — they will be stripped by the domain",
                command.EventId);
        }

        try
        {
            await ingestUseCase.ExecuteAsync(command, ct);
        }
        catch (DuplicateAuditEventException)
        {
            logger.LogDebug(
                "Duplicate audit event {EventId} caught at DB level — ignoring",
                command.EventId);
            return;
        }

        await idempotencyStore.MarkProcessedAsync(command.EventId, ct);
    }

    private static void ValidateRequired(IngestAuditEventCommand command)
    {
        var errors = new List<string>();

        if (command.EventId == Guid.Empty)
            errors.Add("EventId cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.TraceId))
            errors.Add("TraceId cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.Source))
            errors.Add("Source cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.EventType))
            errors.Add("EventType cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.Payload))
            errors.Add("Payload cannot be empty.");

        if (command.OccurredAt == default)
            errors.Add("OccurredAt must be a valid date.");

        if (errors.Count > 0)
            throw new InvalidAuditEnvelopeException(string.Join(" ", errors));
    }
}
