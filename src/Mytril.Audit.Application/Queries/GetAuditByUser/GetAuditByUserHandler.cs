using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByUser;

public sealed class GetAuditByUserHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByUserQuery query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await queryRepository.FindByUserAsync(
            query.UserId,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(AuditEntryDto.From).ToList();

        return new PagedResult<AuditEntryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
