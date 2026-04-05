using Microsoft.EntityFrameworkCore;
using Mytril.Audit.Domain.Entities;

namespace Mytril.Audit.Infrastructure.Persistence;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
    }
}
