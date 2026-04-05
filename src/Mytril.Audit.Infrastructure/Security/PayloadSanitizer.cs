using System.Text.Json;
using Mytril.Audit.Domain.Services;
using Mytril.Audit.Domain.ValueObjects;

namespace Mytril.Audit.Infrastructure.Security;

public sealed class PayloadSanitizer : IPayloadSanitizer
{
    public bool HasForbiddenFields(string json)
    {
        Dictionary<string, JsonElement>? parsed;

        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null)
            return false;

        return parsed.Keys.Any(key => SanitizedPayload.ForbiddenFields.Contains(key));
    }
}
