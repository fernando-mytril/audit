using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mytril.MaterialId.Catalogo.Domain;

namespace Mytril.MaterialId.Catalogo.Infrastructure;

/// <summary>
/// Carga inicial do catálogo a partir das tabelas de referência (Sampaio e Aço Cearense).
/// Idempotente: os IDs são derivados do conteúdo da linha, e itens já existentes não são duplicados.
/// O arquivo é gerado por material-id/tools/gerar_catalogo_seed.py.
/// </summary>
public sealed class CatalogoSeeder(CatalogoDbContext db, ILogger<CatalogoSeeder> logger)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public async Task<int> CarregarAsync(CancellationToken ct)
    {
        var seed = LerArquivo();
        var fabricantesExistentes = await db.Fabricantes.ToDictionaryAsync(f => f.Nome, ct);
        foreach (var f in seed.Fabricantes.Where(f => !fabricantesExistentes.ContainsKey(f.Nome)))
        {
            var novo = new Fabricante(IdDeterministico($"fabricante|{f.Nome}"), f.Nome, f.Fonte, f.Observacao);
            db.Fabricantes.Add(novo);
            fabricantesExistentes[f.Nome] = novo;
        }

        var idsExistentes = (await db.Itens.Select(i => i.Id).ToListAsync(ct)).ToHashSet();
        var novos = 0;
        foreach (var s in seed.Itens)
        {
            var id = IdDeterministico($"item|{s.Fabricante}|{s.Familia}|{s.Acabamento}|{s.EspessuraMm}|{s.DiametroMm}|{s.LarguraMm}|{s.AlturaMm}|{s.AbaMm}|{s.BitolaPol}");
            if (!idsExistentes.Add(id)) continue;
            db.Itens.Add(new ItemCatalogo(id, fabricantesExistentes[s.Fabricante].Id,
                Enum.Parse<Familia>(s.Familia), Enum.Parse<Acabamento>(s.Acabamento), s.EspessuraMm,
                s.DiametroMm, s.LarguraMm, s.AlturaMm, s.AbaMm, s.BitolaPol,
                Enum.Parse<TipoPeso>(s.TipoPeso), s.ValorPeso, s.ComprimentoPadraoMm, s.PecasPorAmarrado,
                s.RequerRevisao, s.Observacao));
            novos++;
        }

        await db.SaveChangesAsync(ct);
        if (novos > 0) logger.LogInformation("Catálogo: {Novos} itens carregados da tabela de referência v{Versao}.", novos, seed.Versao);
        return novos;
    }

    private static SeedArquivo LerArquivo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var nome = assembly.GetManifestResourceNames().Single(n => n.EndsWith("catalogo-seed.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(nome)!;
        return JsonSerializer.Deserialize<SeedArquivo>(stream, Json) ?? throw new InvalidOperationException("Arquivo de carga do catálogo vazio.");
    }

    private static Guid IdDeterministico(string chave) => new(MD5.HashData(Encoding.UTF8.GetBytes(chave)));

    private sealed record SeedArquivo(int Versao, List<SeedFabricante> Fabricantes, List<SeedItem> Itens);
    private sealed record SeedFabricante(string Nome, string? Fonte, string? Observacao);
    private sealed record SeedItem(string Fabricante, string Familia, string Acabamento, decimal EspessuraMm, decimal? DiametroMm,
        decimal? LarguraMm, decimal? AlturaMm, decimal? AbaMm, string? BitolaPol, string TipoPeso, decimal ValorPeso,
        decimal? ComprimentoPadraoMm, int? PecasPorAmarrado, bool RequerRevisao, string? Observacao);
}
