using System.Text.Json;

namespace Mytril.Audit.Sdk.Internal;

internal static class PayloadValidator
{
    private static readonly IReadOnlySet<string> ForbiddenFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Credenciais
            "password", "passwordHash", "newPassword", "confirmPassword",
            "oldPassword", "currentPassword",

            // Tokens e chaves
            "token", "tokenHash", "rawToken", "resetToken",
            "accessToken", "refreshToken", "idToken", "bearerToken",

            // OAuth e secrets
            "appPassword", "clientSecret", "clientId",
            "oauth2RefreshToken", "signingKey", "privateKey", "apiKey",

            // Dados financeiros
            "cvv", "cardNumber", "accountNumber", "bankAccount", "routingNumber",

            // Dados pessoais sensíveis
            "ssn", "taxId", "nationalId", "driversLicense"
        };

    internal static (string SanitizedPayload, List<string> RemovedFields)
        SanitizeAndReport(string json)
    {
        var removedFields = new List<string>();

        try
        {
            var dict = JsonSerializer
                .Deserialize<Dictionary<string, JsonElement>>(json)
                ?? throw new ArgumentException("Invalid JSON payload");

            foreach (var key in dict.Keys.ToList())
            {
                if (ForbiddenFields.Contains(key))
                {
                    dict.Remove(key);
                    removedFields.Add(key);
                }
            }

            return removedFields.Count > 0
                ? (JsonSerializer.Serialize(dict), removedFields)
                : (json, removedFields);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"Audit payload is not valid JSON: {ex.Message}", ex);
        }
    }
}
