using Microsoft.Extensions.Logging.Abstractions;
using Mytril.Audit.Application.UseCases.IngestAuditEvent;
using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Mytril.Audit.Application.Tests.UseCases;

public sealed class IngestAuditEventUseCaseTests
{
    private readonly IAuditRepository _repository = Substitute.For<IAuditRepository>();
    private readonly IngestAuditEventUseCase _useCase;

    public IngestAuditEventUseCaseTests()
    {
        _useCase = new IngestAuditEventUseCase(
            _repository,
            NullLogger<IngestAuditEventUseCase>.Instance);
    }

    private static IngestAuditEventCommand CreateValidCommand() =>
        new(
            EventId: Guid.NewGuid(),
            EventType: "USER_CREATED",
            EventVersion: "1.0",
            Source: "registration-service",
            Severity: AuditSeverity.Info,
            TraceId: "trace-xyz-789",
            Payload: """{"userId":"abc","email":"user@example.com"}""",
            OccurredAt: new DateTime(2026, 3, 20, 14, 0, 0, DateTimeKind.Utc),
            UserId: Guid.NewGuid(),
            ActorId: Guid.NewGuid(),
            TenantId: Guid.NewGuid());

    [Fact]
    public async Task ExecuteAsync_ValidCommand_InsertsEntry()
    {
        var command = CreateValidCommand();

        await _useCase.ExecuteAsync(command);

        await _repository.Received(1)
            .InsertAsync(Arg.Is<AuditEntry>(e =>
                e.EventId == command.EventId &&
                e.TraceId == command.TraceId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateKey_ThrowsDuplicateAuditEventException()
    {
        var command = CreateValidCommand();

        _repository
            .InsertAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("duplicate key value violates unique constraint"));

        await Assert.ThrowsAsync<DuplicateAuditEventException>(
            () => _useCase.ExecuteAsync(command));
    }
}
