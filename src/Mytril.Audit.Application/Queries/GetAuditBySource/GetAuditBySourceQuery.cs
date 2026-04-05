using Mytril.Audit.Domain.Enums;

namespace Mytril.Audit.Application.Queries.GetAuditBySource;

public sealed record GetAuditBySourceQuery(
    string Source,
    DateTime? From,
    DateTime? To,
    AuditSeverity? Severity,
    int Page = 1,
    int PageSize = 50);
