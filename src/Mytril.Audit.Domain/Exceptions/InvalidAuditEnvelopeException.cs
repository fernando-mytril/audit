namespace Mytril.Audit.Domain.Exceptions;

public sealed class InvalidAuditEnvelopeException(string message)
    : DomainException("INVALID_AUDIT_ENVELOPE", message);
