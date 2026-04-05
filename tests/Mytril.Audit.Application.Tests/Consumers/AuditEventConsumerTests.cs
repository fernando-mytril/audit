using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mytril.Audit.Application.Consumers;
using Mytril.Audit.Application.UseCases.IngestAuditEvent;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.Repositories;
using Mytril.Audit.Domain.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Mytril.Audit.Application.Tests.Consumers;

public sealed class AuditEventConsumerTests
{
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly IIdempotencyStore _idempotencyStore = Substitute.For<IIdempotencyStore>();
    private readonly IPayloadSanitizer _payloadSanitizer = Substitute.For<IPayloadSanitizer>();

    private readonly IngestAuditEventUseCase _ingestUseCase;
    private readonly AuditEventConsumer _consumer;

    public AuditEventConsumerTests()
    {
        _ingestUseCase = new IngestAuditEventUseCase(
            _auditRepository,
            NullLogger<IngestAuditEventUseCase>.Instance);

        _consumer = new AuditEventConsumer(
            _ingestUseCase,
            _idempotencyStore,
            _payloadSanitizer,
            NullLogger<AuditEventConsumer>.Instance);
    }

    private static IngestAuditEventCommand CreateValidCommand(Guid? eventId = null) =>
        new(
            EventId: eventId ?? Guid.NewGuid(),
            EventType: "AUTH_LOGIN_SUCCESS",
            EventVersion: "1.0",
            Source: "auth-service",
            Severity: AuditSeverity.Info,
            TraceId: "trace-123",
            Payload: """{"action":"login"}""",
            OccurredAt: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task ConsumeAsync_ValidEvent_IngestsAndMarksProcessed()
    {
        var command = CreateValidCommand();
        _idempotencyStore.ExistsAsync(command.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _payloadSanitizer.HasForbiddenFields(command.Payload).Returns(false);

        await _consumer.ConsumeAsync(command);

        await _auditRepository.Received(1)
            .InsertAsync(Arg.Any<Domain.Entities.AuditEntry>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.Received(1)
            .MarkProcessedAsync(command.EventId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeAsync_DuplicateInRedis_SkipsProcessing()
    {
        var command = CreateValidCommand();
        _idempotencyStore.ExistsAsync(command.EventId, Arg.Any<CancellationToken>())
            .Returns(true);

        await _consumer.ConsumeAsync(command);

        await _auditRepository.DidNotReceive()
            .InsertAsync(Arg.Any<Domain.Entities.AuditEntry>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.DidNotReceive()
            .MarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeAsync_InvalidEnvelope_ThrowsInvalidAuditEnvelopeException()
    {
        var command = new IngestAuditEventCommand(
            EventId: Guid.Empty,
            EventType: "AUTH_LOGIN_SUCCESS",
            EventVersion: "1.0",
            Source: "auth-service",
            Severity: AuditSeverity.Info,
            TraceId: "trace-123",
            Payload: """{"action":"login"}""",
            OccurredAt: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));

        await Assert.ThrowsAsync<InvalidAuditEnvelopeException>(
            () => _consumer.ConsumeAsync(command));
    }

    [Fact]
    public async Task ConsumeAsync_ForbiddenFields_LogsWarningButProcesses()
    {
        var command = CreateValidCommand();
        _idempotencyStore.ExistsAsync(command.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _payloadSanitizer.HasForbiddenFields(command.Payload).Returns(true);

        await _consumer.ConsumeAsync(command);

        await _auditRepository.Received(1)
            .InsertAsync(Arg.Any<Domain.Entities.AuditEntry>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.Received(1)
            .MarkProcessedAsync(command.EventId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeAsync_DuplicateInDb_CaughtSilently()
    {
        var eventId = Guid.NewGuid();
        var command = CreateValidCommand(eventId);
        _idempotencyStore.ExistsAsync(command.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _payloadSanitizer.HasForbiddenFields(command.Payload).Returns(false);

        _auditRepository
            .InsertAsync(Arg.Any<Domain.Entities.AuditEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("duplicate key value violates unique constraint"));

        // Should NOT propagate — consumer catches DuplicateAuditEventException
        await _consumer.ConsumeAsync(command);

        await _idempotencyStore.DidNotReceive()
            .MarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
