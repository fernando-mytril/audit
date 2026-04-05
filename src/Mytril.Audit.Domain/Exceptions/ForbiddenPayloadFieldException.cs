namespace Mytril.Audit.Domain.Exceptions;

public sealed class ForbiddenPayloadFieldException(string fieldName)
    : DomainException("FORBIDDEN_PAYLOAD_FIELD", $"Payload contains forbidden field: '{fieldName}'.");
