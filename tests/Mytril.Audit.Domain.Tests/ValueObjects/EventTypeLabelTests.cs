using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Domain.Tests.ValueObjects;

public sealed class EventTypeLabelTests
{
    [Theory]
    [InlineData("AUTH_LOGIN_SUCCESS")]
    [InlineData("USER_CREATED")]
    [InlineData("PAYMENT.PROCESSED")]
    public void Constructor_WithValidType_Succeeds(string value)
    {
        var label = new EventTypeLabel(value);

        Assert.Equal(value.ToUpperInvariant(), label.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmpty_Throws(string? value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new EventTypeLabel(value!));
    }

    [Theory]
    [InlineData("AUTH LOGIN")]
    [InlineData("user created")]
    public void Constructor_WithSpaces_Throws(string value)
    {
        Assert.Throws<InvalidAuditEnvelopeException>(() => new EventTypeLabel(value));
    }

    [Fact]
    public void Constructor_NormalizesToUpperCase()
    {
        var label = new EventTypeLabel("auth_login");

        Assert.Equal("AUTH_LOGIN", label.Value);
    }
}
