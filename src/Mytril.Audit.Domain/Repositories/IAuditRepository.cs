using Mytril.Audit.Domain.Entities;

namespace Mytril.Audit.Domain.Repositories;

public interface IAuditRepository
{
    Task InsertAsync(AuditEntry entry, CancellationToken ct = default);
}
