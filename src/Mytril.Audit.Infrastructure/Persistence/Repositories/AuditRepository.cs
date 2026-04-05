using Mytril.Audit.Domain.Entities;
using Mytril.Audit.Domain.Repositories;

namespace Mytril.Audit.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(AuditDbContext dbContext) : IAuditRepository
{
    public async Task InsertAsync(AuditEntry entry, CancellationToken ct = default)
    {
        dbContext.AuditLog.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }
}
