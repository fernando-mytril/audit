using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Domain.Tests.ValueObjects;

public sealed class EventSourceTests
{
    [Theory]
    [InlineData("mytril-billing")]
    [InlineData("auth-service")]
    [InlineData("my-app-v2")]
    [InlineData("ab")]
    public void Constructor_WithValidKebabCase_Succeeds(string value)
    {
        var source = new EventSource(value);

        Assert.Equal(value, source.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmpty_Throws(string? value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new EventSource(value!));
    }

    [Theory]
    [InlineData("mytril billing")]
    [InlineData("auth service")]
    public void Constructor_WithSpaces_Throws(string value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new EventSource(value));
    }

    [Theory]
    [InlineData("Mytril-Billing")]
    [InlineData("Auth-Service")]
    public void Constructor_WithUpperCase_NormalizesToLower(string value)
    {
        // EventSource normalizes to lowercase before regex check,
        // so uppercase input is accepted and stored as lowercase.
        var source = new EventSource(value);

        Assert.Equal(value.ToLowerInvariant(), source.Value);
    }
}
