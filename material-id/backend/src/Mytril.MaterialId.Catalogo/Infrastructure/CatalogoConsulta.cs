using Microsoft.EntityFrameworkCore;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Catalogo.Domain;
using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Catalogo.Infrastructure;

public sealed class CatalogoConsulta(CatalogoDbContext db) : ICatalogoConsulta
{
    public async Task<IReadOnlyList<FabricanteDto>> ListarFabricantesAsync(CancellationToken ct) =>
        await db.Fabricantes.AsNoTracking().Where(f => f.Ativo).OrderBy(f => f.Nome)
            .Select(f => new FabricanteDto(f.Id, f.Nome, f.Fonte, f.Observacao)).ToListAsync(ct);

    public async Task<IReadOnlyList<ItemCatalogoDto>> ListarItensAsync(FiltroCatalogo filtro, CancellationToken ct)
    {
        var query = from i in db.Itens.AsNoTracking()
                    join f in db.Fabricantes.AsNoTracking() on i.FabricanteId equals f.Id
                    where i.Ativo
                    select new { i, f.Nome };

        if (filtro.FabricanteId is { } fab) query = query.Where(x => x.i.FabricanteId == fab);
        if (filtro.Familia is { } fam) query = query.Where(x => x.i.Familia == fam);
        if (filtro.RequerRevisao is { } rev) query = query.Where(x => x.i.RequerRevisao == rev);

        var lista = await query.ToListAsync(ct);
        IEnumerable<ItemCatalogoDto> itens = lista.Select(x => Map(x.i, x.Nome));
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termos = filtro.Busca.Trim().ToLowerInvariant().Replace('x', '×').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            itens = itens.Where(i => termos.All(t => i.Descricao.ToLowerInvariant().Contains(t) || i.Fabricante.ToLowerInvariant().Contains(t)));
        }

        return itens.OrderBy(i => i.Fabricante).ThenBy(i => i.Familia).ThenBy(i => i.DiametroMm ?? i.LarguraMm)
            .ThenBy(i => i.AlturaMm).ThenBy(i => i.EspessuraMm).ToList();
    }

    public async Task<ItemCatalogoDto?> ObterItemAsync(Guid id, CancellationToken ct)
    {
        var x = await (from i in db.Itens.AsNoTracking()
                       join f in db.Fabricantes.AsNoTracking() on i.FabricanteId equals f.Id
                       where i.Id == id
                       select new { i, f.Nome }).FirstOrDefaultAsync(ct);
        return x is null ? null : Map(x.i, x.Nome);
    }

    public async Task<CalculoPesoDto> CalcularPesoAsync(Guid itemId, int quantidade, decimal? comprimentoMm, decimal? larguraMm, CancellationToken ct)
    {
        var item = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId, ct)
                   ?? throw new NaoEncontradoException($"Item de catálogo {itemId} não encontrado.");
        var peso = CalculadoraPeso.CalcularKg(item, quantidade, comprimentoMm, larguraMm);
        return new CalculoPesoDto(item.Id, item.Descricao, quantidade, comprimentoMm, larguraMm, peso);
    }

    private static ItemCatalogoDto Map(ItemCatalogo i, string fabricante) => new(
        i.Id, i.FabricanteId, fabricante, i.Familia, i.Acabamento, i.Descricao, i.EspessuraMm, i.DiametroMm, i.LarguraMm,
        i.AlturaMm, i.AbaMm, i.Bitola, i.TipoPeso, i.ValorPeso, i.ComprimentoPadraoMm, i.PecasPorAmarrado, i.RequerRevisao, i.Observacao);
}
