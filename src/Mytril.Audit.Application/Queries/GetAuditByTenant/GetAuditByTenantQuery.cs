namespace Mytril.Audit.Application.Queries.GetAuditByTenant;

public sealed record GetAuditByTenantQuery(
    Guid TenantId,
    string? Source,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
