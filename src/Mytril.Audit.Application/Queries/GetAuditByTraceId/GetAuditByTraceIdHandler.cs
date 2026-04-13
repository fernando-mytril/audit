using System.Diagnostics;
using Mytril.Audit.Application.Diagnostics;
using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Application.Queries.GetAuditByTraceId;

public sealed class GetAuditByTraceIdHandler(IAuditQueryRepository queryRepository)
{
    public async Task<PagedResult<AuditEntryDto>> HandleAsync(
        GetAuditByTraceIdQuery query,
        CancellationToken ct = default)
    {
        using var activity = AuditDiagnostics.ActivitySource.StartActivity("QueryByTraceId");
        activity?.SetTag("audit.query.trace_id", query.TraceId);

        var sw = Stopwatch.StartNew();
        try
        {
            var (items, totalCount) = await queryRepository.FindByTraceIdAsync(query.TraceId, ct);
            var dtos = items.Select(AuditEntryDto.From).ToList();

            AuditDiagnostics.QueryExecuted.Add(1,
                new KeyValuePair<string, object?>("query", "ByTraceId"));
            activity?.SetStatus(ActivityStatusCode.Ok);
            return new PagedResult<AuditEntryDto>(dtos, totalCount, Page: 1, PageSize: 1000);
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
                new KeyValuePair<string, object?>("usecase", "QueryByTraceId"));
        }
    }
}
