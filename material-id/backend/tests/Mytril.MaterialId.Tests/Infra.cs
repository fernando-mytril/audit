using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mytril.MaterialId.Catalogo.Infrastructure;
using Mytril.MaterialId.Conferencias.Infrastructure;
using Mytril.MaterialId.SharedKernel;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Tests;

public sealed class ContextoFixo(Guid empresaId, string usuario = "teste") : IContextoUsuario
{
    public Guid EmpresaId { get; } = empresaId;
    public string Usuario { get; } = usuario;
}

public sealed class RelogioFixo : IRelogio
{
    public DateTimeOffset Agora { get; set; } = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
}

/// <summary>Banco SQLite em memória compartilhado entre contextos do mesmo teste.</summary>
public sealed class BancoEmMemoria : IDisposable
{
    public SqliteConnection Conexao { get; } = new("DataSource=:memory:");

    public BancoEmMemoria()
    {
        Conexao.Open();
        using (var c = Catalogo()) c.Database.EnsureCreated();
        using (var c = Conferencias(Guid.NewGuid()))
        {
            // Cria só as tabelas do módulo Conferências no mesmo arquivo.
            var script = c.Database.GenerateCreateScript();
            foreach (var cmd in script.Split(";", StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)))
                c.Database.ExecuteSqlRaw(cmd);
        }
    }

    public CatalogoDbContext Catalogo() => new(new DbContextOptionsBuilder<CatalogoDbContext>().UseSqlite(Conexao).Options);

    public ConferenciasDbContext Conferencias(Guid empresaId) =>
        new(new DbContextOptionsBuilder<ConferenciasDbContext>().UseSqlite(Conexao).Options, new ContextoFixo(empresaId));

    public async Task<CatalogoDbContext> CatalogoCarregadoAsync()
    {
        var db = Catalogo();
        await new CatalogoSeeder(db, NullLogger<CatalogoSeeder>.Instance).CarregarAsync(CancellationToken.None);
        return db;
    }

    public void Dispose() => Conexao.Dispose();
}
