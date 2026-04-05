using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByFilter;

public sealed class GetAuditByFilterHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByFilterQuery query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await queryRepository.FindByFilterAsync(
            query.Source,
            query.EventType,
            query.Severity,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(AuditEntryDto.From).ToList();

        return new PagedResult<AuditEntryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
