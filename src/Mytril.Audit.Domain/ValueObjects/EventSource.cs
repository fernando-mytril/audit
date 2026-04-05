using System.Text.RegularExpressions;
using Mytril.Audit.Domain.Exceptions;

namespace Mytril.Audit.Domain.ValueObjects;

public partial record EventSource
{
    private static readonly Regex KebabCaseRegex = KebabCasePattern();

    public string Value { get; }

    public EventSource(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidAuditEnvelopeException("EventSource cannot be null or empty.");

        var normalized = value.ToLowerInvariant();

        if (!KebabCaseRegex.IsMatch(normalized))
            throw new InvalidAuditEnvelopeException(
                $"EventSource '{value}' must be kebab-case (e.g. 'auth-service').");

        Value = normalized;
    }

    public static implicit operator string(EventSource source) => source.Value;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$", RegexOptions.Compiled)]
    private static partial Regex KebabCasePattern();
}
