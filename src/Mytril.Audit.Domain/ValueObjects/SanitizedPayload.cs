using System.Text.Json;
using Mytril.Audit.Domain.Exceptions;

namespace Mytril.Audit.Domain.ValueObjects;

public record SanitizedPayload
{
    public static readonly IReadOnlySet<string> ForbiddenFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "newPassword",
        "confirmPassword",
        "oldPassword",
        "currentPassword",
        "token",
        "tokenHash",
        "rawToken",
        "resetToken",
        "accessToken",
        "refreshToken",
        "idToken",
        "appPassword",
        "clientSecret",
        "clientId",
        "oauth2RefreshToken",
        "bearerToken",
        "secret",
        "apiKey",
        "privateKey",
        "signingKey",
        "cvv",
        "cardNumber",
        "accountNumber",
        "ssn",
        "taxId",
        "nationalId"
    };

    public string Value { get; }

    public SanitizedPayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidAuditEnvelopeException("Payload cannot be null or empty.");

        Dictionary<string, JsonElement> parsed;

        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value)
                ?? throw new InvalidAuditEnvelopeException("Payload must be a valid JSON object.");
        }
        catch (JsonException)
        {
            throw new InvalidAuditEnvelopeException("Payload must be a valid JSON object.");
        }

        var keysToRemove = parsed.Keys
            .Where(k => ForbiddenFields.Contains(k))
            .ToList();

        if (keysToRemove.Count > 0)
        {
            foreach (var key in keysToRemove)
                parsed.Remove(key);

            Value = JsonSerializer.Serialize(parsed);
        }
        else
        {
            Value = value;
        }
    }

    public static implicit operator string(SanitizedPayload payload) => payload.Value;
}
