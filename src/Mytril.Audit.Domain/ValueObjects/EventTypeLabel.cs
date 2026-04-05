using Mytril.Audit.Domain.Exceptions;

namespace Mytril.Audit.Domain.ValueObjects;

public record EventTypeLabel
{
    public string Value { get; }

    public EventTypeLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidAuditEnvelopeException("EventTypeLabel cannot be null or empty.");

        if (value.Contains(' '))
            throw new InvalidAuditEnvelopeException(
                $"EventTypeLabel '{value}' must not contain spaces.");

        Value = value.ToUpperInvariant();
    }

    public static implicit operator string(EventTypeLabel label) => label.Value;
}
