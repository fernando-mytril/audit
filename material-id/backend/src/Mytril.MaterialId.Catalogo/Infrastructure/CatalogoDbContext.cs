using Microsoft.EntityFrameworkCore;
using Mytril.MaterialId.Catalogo.Domain;

namespace Mytril.MaterialId.Catalogo.Infrastructure;

/// <summary>Catálogo é dado da plataforma, compartilhado entre empresas (D26). Schema próprio no PostgreSQL.</summary>
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public const string Schema = "catalogo";

    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();
    public DbSet<ItemCatalogo> Itens => Set<ItemCatalogo>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema(Schema);

        model.Entity<Fabricante>(e =>
        {
            e.ToTable("fabricantes");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).ValueGeneratedNever();
            e.Property(f => f.Nome).HasMaxLength(120).IsRequired();
            e.HasIndex(f => f.Nome).IsUnique();
            e.Property(f => f.Fonte).HasMaxLength(500);
            e.Property(f => f.Observacao).HasMaxLength(500);
        });

        model.Entity<ItemCatalogo>(e =>
        {
            e.ToTable("itens");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedNever();
            e.HasOne<Fabricante>().WithMany().HasForeignKey(i => i.FabricanteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(i => i.Familia).HasConversion<string>().HasMaxLength(30);
            e.Property(i => i.Acabamento).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.TipoPeso).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.EspessuraMm).HasPrecision(7, 2);
            e.Property(i => i.DiametroMm).HasPrecision(8, 2);
            e.Property(i => i.LarguraMm).HasPrecision(8, 2);
            e.Property(i => i.AlturaMm).HasPrecision(8, 2);
            e.Property(i => i.AbaMm).HasPrecision(8, 2);
            e.Property(i => i.ValorPeso).HasPrecision(12, 4);
            e.Property(i => i.ComprimentoPadraoMm).HasPrecision(8, 1);
            e.Property(i => i.Bitola).HasMaxLength(20);
            e.Property(i => i.Descricao).HasMaxLength(200);
            e.Property(i => i.Observacao).HasMaxLength(500);
            e.HasIndex(i => new { i.FabricanteId, i.Familia });
        });
    }
}
