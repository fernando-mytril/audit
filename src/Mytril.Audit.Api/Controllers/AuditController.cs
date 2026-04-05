using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mytril.Audit.Api.Responses;
using Mytril.Audit.Application.DTOs;
using Mytril.Audit.Application.Queries.GetAuditByActor;
using Mytril.Audit.Application.Queries.GetAuditByFilter;
using Mytril.Audit.Application.Queries.GetAuditBySource;
using Mytril.Audit.Application.Queries.GetAuditByTenant;
using Mytril.Audit.Application.Queries.GetAuditByTraceId;
using Mytril.Audit.Application.Queries.GetAuditByUser;
using Mytril.Audit.Domain.Enums;

namespace Mytril.Audit.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Policy = "RequireAuditReadPermission")]
[Produces("application/json")]
public sealed class AuditController(
    GetAuditByTraceIdHandler traceIdHandler,
    GetAuditBySourceHandler sourceHandler,
    GetAuditByActorHandler actorHandler,
    GetAuditByUserHandler userHandler,
    GetAuditByTenantHandler tenantHandler,
    GetAuditByFilterHandler filterHandler) : ControllerBase
{
    [HttpGet("trace/{traceId}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByTraceId(
        string traceId,
        CancellationToken ct)
    {
        var query = new GetAuditByTraceIdQuery(traceId);
        var result = await traceIdHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("source/{source}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBySource(
        string source,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AuditSeverity? severity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetAuditBySourceQuery(source, from, to, severity, page, pageSize);
        var result = await sourceHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("actor/{actorId:guid}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByActor(
        Guid actorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetAuditByActorQuery(actorId, from, to, page, pageSize);
        var result = await actorHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByUser(
        Guid userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetAuditByUserQuery(userId, from, to, page, pageSize);
        var result = await userHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByTenant(
        Guid tenantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetAuditByTenantQuery(tenantId, source, from, to, page, pageSize);
        var result = await tenantHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("events")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByFilter(
        [FromQuery] string? source,
        [FromQuery] string? eventType,
        [FromQuery] AuditSeverity? severity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetAuditByFilterQuery(source, eventType, severity, from, to, page, pageSize);
        var result = await filterHandler.HandleAsync(query, ct);
        return Ok(result);
    }
}
