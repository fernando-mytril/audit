namespace Mytril.Audit.Application.Queries.GetAuditByActor;

public sealed record GetAuditByActorQuery(
    Guid ActorId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
