using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Exceptions;

namespace Mytril.Audit.Domain.Tests.Entities;

public sealed class AuditEntryTests
{
    private static readonly Guid ValidEventId = Guid.NewGuid();
    private const string ValidEventType = "AUTH_LOGIN_SUCCESS";
    private const string ValidEventVersion = "1.0";
    private const string ValidSource = "auth-service";
    private const AuditSeverity ValidSeverity = AuditSeverity.Info;
    private const string ValidTraceId = "trace-abc-123";
    private const string ValidPayload = """{"action":"login","ip":"10.0.0.1"}""";
    private static readonly DateTime ValidOccurredAt = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_ReturnsAuditEntry()
    {
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenantProductId = Guid.NewGuid();

        var entry = AuditEntry.Create(
            ValidEventId,
            ValidEventType,
            ValidEventVersion,
            ValidSource,
            ValidSeverity,
            ValidTraceId,
            ValidPayload,
            ValidOccurredAt,
            userId,
            actorId,
            tenantId,
            tenantProductId,
            "192.168.1.1",
            "Mozilla/5.0",
            "corr-456");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(ValidEventId, entry.EventId);
        Assert.Equal(ValidEventType, entry.EventType.Value);
        Assert.Equal(ValidEventVersion, entry.EventVersion);
        Assert.Equal(ValidSource, entry.Source.Value);
        Assert.Equal(ValidSeverity, entry.Severity);
        Assert.Equal(ValidTraceId, entry.TraceId);
        Assert.Equal(ValidOccurredAt, entry.OccurredAt);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(actorId, entry.ActorId);
        Assert.Equal(tenantId, entry.TenantId);
        Assert.Equal(tenantProductId, entry.TenantProductId);
        Assert.Equal("192.168.1.1", entry.IpAddress);
        Assert.Equal("Mozilla/5.0", entry.UserAgent);
        Assert.Equal("corr-456", entry.CorrelationId);
        Assert.True(entry.ReceivedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyEventId_ThrowsInvalidAuditEnvelopeException()
    {
        var ex = Assert.Throws<InvalidAuditEnvelopeException>(() =>
            AuditEntry.Create(
                Guid.Empty,
                ValidEventType,
                ValidEventVersion,
                ValidSource,
                ValidSeverity,
                ValidTraceId,
                ValidPayload,
                ValidOccurredAt));

        Assert.Contains("EventId", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyTraceId_ThrowsInvalidAuditEnvelopeException()
    {
        var ex = Assert.Throws<InvalidAuditEnvelopeException>(() =>
            AuditEntry.Create(
                ValidEventId,
                ValidEventType,
                ValidEventVersion,
                ValidSource,
                ValidSeverity,
                "",
                ValidPayload,
                ValidOccurredAt));

        Assert.Contains("TraceId", ex.Message);
    }

    [Fact]
    public void Create_WithEmptySource_ThrowsInvalidAuditEnvelopeException()
    {
        var ex = Assert.Throws<InvalidAuditEnvelopeException>(() =>
            AuditEntry.Create(
                ValidEventId,
                ValidEventType,
                ValidEventVersion,
                "",
                ValidSeverity,
                ValidTraceId,
                ValidPayload,
                ValidOccurredAt));

        Assert.Contains("Source", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyEventType_ThrowsInvalidAuditEnvelopeException()
    {
        var ex = Assert.Throws<InvalidAuditEnvelopeException>(() =>
            AuditEntry.Create(
                ValidEventId,
                "",
                ValidEventVersion,
                ValidSource,
                ValidSeverity,
                ValidTraceId,
                ValidPayload,
                ValidOccurredAt));

        Assert.Contains("EventType", ex.Message);
    }

    [Fact]
    public void Create_WithDefaultOccurredAt_ThrowsInvalidAuditEnvelopeException()
    {
        var ex = Assert.Throws<InvalidAuditEnvelopeException>(() =>
            AuditEntry.Create(
                ValidEventId,
                ValidEventType,
                ValidEventVersion,
                ValidSource,
                ValidSeverity,
                ValidTraceId,
                ValidPayload,
                default));

        Assert.Contains("OccurredAt", ex.Message);
    }
}
