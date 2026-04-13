using System.Diagnostics;
using Mytril.Audit.Application.Diagnostics;
using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByFilter;

public sealed class GetAuditByFilterHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByFilterQuery query,
        CancellationToken ct = default)
    {
        using var activity = AuditDiagnostics.ActivitySource.StartActivity("QueryByFilter");

        var sw = Stopwatch.StartNew();
        try
        {
            var (items, totalCount) = await queryRepository.FindByFilterAsync(
                query.Source, query.EventType, query.Severity,
                query.From, query.To, query.Page, query.PageSize, ct);
            var dtos = items.Select(AuditEntryDto.From).ToList();

            AuditDiagnostics.QueryExecuted.Add(1,
                new KeyValuePair<string, object?>("query", "ByFilter"));
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
                new KeyValuePair<string, object?>("usecase", "QueryByFilter"));
        }
    }
}
