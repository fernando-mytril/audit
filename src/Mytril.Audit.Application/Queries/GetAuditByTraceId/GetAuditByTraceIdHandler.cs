using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByTraceId;

public sealed class GetAuditByTraceIdHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByTraceIdQuery query,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await queryRepository.FindByTraceIdAsync(query.TraceId, ct);

        var dtos = items.Select(AuditEntryDto.From).ToList();

        return new PagedResult<AuditEntryDto>(dtos, totalCount, Page: 1, PageSize: 1000);
    }
}
