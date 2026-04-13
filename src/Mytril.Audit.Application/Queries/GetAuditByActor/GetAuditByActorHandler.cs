using System.Diagnostics;
using Mytril.Audit.Application.Diagnostics;
using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByActor;

public sealed class GetAuditByActorHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByActorQuery query,
        CancellationToken ct = default)
    {
        using var activity = AuditDiagnostics.ActivitySource.StartActivity("QueryByActor");

        var sw = Stopwatch.StartNew();
        try
        {
            var (items, totalCount) = await queryRepository.FindByActorAsync(
                query.ActorId, query.From, query.To, query.Page, query.PageSize, ct);
            var dtos = items.Select(AuditEntryDto.From).ToList();

            AuditDiagnostics.QueryExecuted.Add(1,
                new KeyValuePair<string, object?>("query", "ByActor"));
            activity?.SetStatus(ActivityStatusCode.Ok);
            return new PagedResult<AuditEntryDto>(dtos, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            sw.Stop();
            AuditDiagnostics.UseCaseDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("usecase", "QueryByActor"));
        }
    }
}
