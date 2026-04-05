using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByActor;

public sealed class GetAuditByActorHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByActorQuery query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await queryRepository.FindByActorAsync(
            query.ActorId,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(AuditEntryDto.From).ToList();

        return new PagedResult<AuditEntryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
