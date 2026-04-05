using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Domain.Tests.ValueObjects;

public sealed class SanitizedPayloadTests
{
    [Fact]
    public void Constructor_WithValidJson_ReturnsPayload()
    {
        const string json = """{"action":"login","ip":"10.0.0.1"}""";

        var payload = new SanitizedPayload(json);

        Assert.Contains("action", payload.Value);
        Assert.Contains("login", payload.Value);
        Assert.Contains("ip", payload.Value);
    }

    [Fact]
    public void Constructor_WithForbiddenField_RemovesField()
    {
        const string json = """{"action":"login","password":"secret123","ip":"10.0.0.1"}""";

        var payload = new SanitizedPayload(json);

        Assert.DoesNotContain("password", payload.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret123", payload.Value);
        Assert.Contains("action", payload.Value);
        Assert.Contains("ip", payload.Value);
    }

    [Fact]
    public void Constructor_WithMultipleForbiddenFields_RemovesAll()
    {
        const string json = """{"action":"login","password":"s1","tokenHash":"abc","accessToken":"xyz","email":"user@test.com"}""";

        var payload = new SanitizedPayload(json);

        Assert.DoesNotContain("password", payload.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", payload.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", payload.Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action", payload.Value);
        Assert.Contains("email", payload.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyPayload_Throws(string? value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new SanitizedPayload(value!));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{invalid")]
    [InlineData("12345")]
    public void Constructor_WithInvalidJson_Throws(string value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new SanitizedPayload(value));
    }
}
