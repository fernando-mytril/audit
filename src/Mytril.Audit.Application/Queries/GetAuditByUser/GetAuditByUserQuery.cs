namespace Mytril.Audit.Application.Queries.GetAuditByUser;

public sealed record GetAuditByUserQuery(
    Guid UserId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
