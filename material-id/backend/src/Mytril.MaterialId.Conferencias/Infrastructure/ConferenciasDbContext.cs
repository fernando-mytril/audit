using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.SharedKernel.StateMachines;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Conferencias.Infrastructure;

/// <summary>
/// Dados comerciais, isolados por empresa (E1-03): filtro global em toda consulta
/// e verificação na gravação, para que nenhuma operação alcance dados de outra empresa.
/// </summary>
public sealed class ConferenciasDbContext(DbContextOptions<ConferenciasDbContext> options, IContextoUsuario contexto) : DbContext(options)
{
    public const string Schema = "conferencias";

    public DbSet<Conferencia> Conferencias => Set<Conferencia>();
    public DbSet<Analise> Analises => Set<Analise>();

    private Guid EmpresaAtual => contexto.EmpresaId;

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite (usado só nos testes) não ordena DateTimeOffset; grava em ticks. No PostgreSQL usa timestamptz.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            builder.Properties<DateTimeOffset>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema(Schema);

        model.Entity<Conferencia>(e =>
        {
            e.ToTable("conferencias");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(c => new { c.EmpresaId, c.Codigo }).IsUnique();
            e.HasIndex(c => new { c.EmpresaId, c.Status });
            e.Property(c => c.Operacao).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Contraparte).HasMaxLength(150);
            e.Property(c => c.CriadaPor).HasMaxLength(150);
            e.Ignore(c => c.GatilhosValidos);
            e.Ignore(c => c.AnalisesAtivas);
            e.Ignore(c => c.QuantidadePecas);
            e.Ignore(c => c.PesoMedidoKg);
            e.HasQueryFilter(c => c.EmpresaId == EmpresaAtual);

            e.HasMany(c => c.Analises).WithOne().HasForeignKey(a => a.ConferenciaId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(c => c.Analises).HasField("_analises").UsePropertyAccessMode(PropertyAccessMode.Field);
            MapearTransicoes(e.OwnsMany(c => c.Transicoes), "conferencia_transicoes", "conferencia_id");
            e.Navigation(c => c.Transicoes).HasField("_transicoes").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        model.Entity<Analise>(e =>
        {
            e.ToTable("analises");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.HasIndex(a => new { a.EmpresaId, a.Status });
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(a => a.Confianca).HasPrecision(5, 4);
            e.Property(a => a.VersaoModelo).HasMaxLength(50);
            e.Property(a => a.MotivoFalha).HasMaxLength(500);
            MapearMedicao(e.OwnsOne(a => a.Resultado), "resultado");
            MapearMedicao(e.OwnsOne(a => a.Correcao), "correcao");
            e.Ignore(a => a.MedicaoAtual);
            e.Ignore(a => a.Ativa);
            e.Ignore(a => a.GatilhosValidos);
            e.HasQueryFilter(a => a.EmpresaId == EmpresaAtual);
            MapearTransicoes(e.OwnsMany(a => a.Transicoes), "analise_transicoes", "analise_id");
            e.Navigation(a => a.Transicoes).HasField("_transicoes").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GarantirEmpresa();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GarantirEmpresa();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Nenhuma gravação de dado de outra empresa, mesmo que um bug carregue a entidade.</summary>
    private void GarantirEmpresa()
    {
        foreach (var entry in ChangeTracker.Entries<IPertenceAEmpresa>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && entry.Entity.EmpresaId != EmpresaAtual)
                throw new UnauthorizedAccessException("Tentativa de gravar dados de outra empresa.");
        }
    }

    private static void MapearMedicao<T>(OwnedNavigationBuilder<T, Medicao> b, string prefixo) where T : class
    {
        b.Property(m => m.ItemCatalogoId).HasColumnName($"{prefixo}_item_catalogo_id");
        b.Property(m => m.Quantidade).HasColumnName($"{prefixo}_quantidade");
        b.Property(m => m.ComprimentoMm).HasColumnName($"{prefixo}_comprimento_mm").HasPrecision(8, 1);
        b.Property(m => m.LarguraMm).HasColumnName($"{prefixo}_largura_mm").HasPrecision(8, 1);
        b.Property(m => m.PesoKg).HasColumnName($"{prefixo}_peso_kg").HasPrecision(12, 3);
    }

    private static void MapearTransicoes<T>(OwnedNavigationBuilder<T, Transicao> b, string tabela, string fk) where T : class
    {
        b.ToTable(tabela);
        b.WithOwner().HasForeignKey(fk);
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).ValueGeneratedNever();
        b.Property(t => t.De).HasMaxLength(30);
        b.Property(t => t.Para).HasMaxLength(30);
        b.Property(t => t.Gatilho).HasMaxLength(30);
        b.Property(t => t.Usuario).HasMaxLength(150);
        b.Property(t => t.Motivo).HasMaxLength(500);
    }
}
