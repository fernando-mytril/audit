using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mytril.Audit.Application.Diagnostics;
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
        using var activity = AuditDiagnostics.ActivitySource.StartActivity("IngestAuditEvent");
        activity?.SetTag("audit.source", command.Source);
        activity?.SetTag("audit.event_type", command.EventType);

        var sw = Stopwatch.StartNew();
        try
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
                AuditDiagnostics.EventDuplicate.Add(1,
                    new KeyValuePair<string, object?>("source", command.Source));
                logger.LogDebug(
                    "Duplicate audit event detected at DB level for EventId {EventId}",
                    command.EventId);
                throw new DuplicateAuditEventException(command.EventId);
            }

            AuditDiagnostics.EventIngested.Add(1,
                new KeyValuePair<string, object?>("source", command.Source),
                new KeyValuePair<string, object?>("event_type", command.EventType));
            activity?.SetStatus(ActivityStatusCode.Ok);

            logger.LogInformation(
                "Audit event ingested — EventId: {EventId}, EventType: {EventType}, Source: {Source}",
                command.EventId, command.EventType, command.Source);
        }
        catch (DuplicateAuditEventException)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            sw.Stop();
            AuditDiagnostics.UseCaseDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("usecase", "IngestAuditEvent"));
        }
    }
}
