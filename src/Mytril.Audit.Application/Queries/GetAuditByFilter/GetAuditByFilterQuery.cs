using Mytril.Audit.Domain.Enums;

namespace Mytril.Audit.Application.Queries.GetAuditByFilter;

public sealed record GetAuditByFilterQuery(
    string? Source,
    string? EventType,
    AuditSeverity? Severity,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
