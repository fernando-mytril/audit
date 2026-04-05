namespace Mytril.Audit.Domain.Exceptions;

public sealed class DuplicateAuditEventException(Guid eventId)
    : DomainException("DUPLICATE_AUDIT_EVENT", $"Audit event with id '{eventId}' has already been processed.");
