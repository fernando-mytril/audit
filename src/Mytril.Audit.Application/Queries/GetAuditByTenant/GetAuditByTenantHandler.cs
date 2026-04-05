using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByTenant;

public sealed class GetAuditByTenantHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByTenantQuery query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await queryRepository.FindByTenantAsync(
            query.TenantId,
            query.Source,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(AuditEntryDto.From).ToList();

        return new PagedResult<AuditEntryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
