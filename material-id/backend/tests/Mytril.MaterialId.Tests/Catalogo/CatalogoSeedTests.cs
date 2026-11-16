using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Catalogo.Domain;
using Mytril.MaterialId.Catalogo.Infrastructure;

namespace Mytril.MaterialId.Tests.Catalogo;

/// <summary>Carga das tabelas da Sampaio e da Aço Cearense (E2-01, MYT-157).</summary>
public class CatalogoSeedTests
{
    [Fact]
    public async Task Carrega_todas_as_linhas_das_tabelas_de_referencia()
    {
        using var banco = new BancoEmMemoria();
        await using var db = await banco.CatalogoCarregadoAsync();

        Assert.Equal(2, await db.Fabricantes.CountAsync());
        Assert.Equal(587, await db.Itens.CountAsync());
        Assert.Equal(48, await db.Itens.CountAsync(i => i.Familia == Familia.TuboRetangular && i.TipoPeso == TipoPeso.KgPorMetro));
        foreach (var familia in Enum.GetValues<Familia>())
            Assert.True(await db.Itens.AnyAsync(i => i.Familia == familia), $"Família {familia} sem itens");
    }

    [Fact]
    public async Task Carga_e_idempotente()
    {
        using var banco = new BancoEmMemoria();
        await using var db = await banco.CatalogoCarregadoAsync();
        var segunda = await new CatalogoSeeder(db, NullLogger<CatalogoSeeder>.Instance).CarregarAsync(CancellationToken.None);
        Assert.Equal(0, segunda);
        Assert.Equal(587, await db.Itens.CountAsync());
    }

    [Fact]
    public async Task Linhas_incoerentes_da_fonte_ficam_marcadas_para_revisao()
    {
        using var banco = new BancoEmMemoria();
        await using var db = await banco.CatalogoCarregadoAsync();
        var revisar = await db.Itens.Where(i => i.RequerRevisao).ToListAsync();

        Assert.Equal(2, revisar.Count);
        Assert.Contains(revisar, i => i.Familia == Familia.TuboRetangular && i.LarguraMm == 50 && i.AlturaMm == 20 && i.EspessuraMm == 0.90m);
        Assert.Contains(revisar, i => i.Familia == Familia.TuboQuadrado && i.LarguraMm == 15 && i.EspessuraMm == 0.60m);
    }

    [Fact]
    public async Task Busca_por_medida_encontra_o_item_e_calcula_o_peso_da_tabela()
    {
        using var banco = new BancoEmMemoria();
        await using var db = await banco.CatalogoCarregadoAsync();
        var consulta = new CatalogoConsulta(db);

        var fabricantes = await consulta.ListarFabricantesAsync(CancellationToken.None);
        var acoCearense = fabricantes.Single(f => f.Nome == "Aço Cearense");
        var itens = await consulta.ListarItensAsync(new FiltroCatalogo(acoCearense.Id, Familia.TuboRetangular, "40x20 1,20"), CancellationToken.None);

        var item = Assert.Single(itens);
        Assert.Equal(98, item.PecasPorAmarrado);
        var peso = await consulta.CalcularPesoAsync(item.Id, 98, null, null, CancellationToken.None);
        Assert.Equal(642.096m, peso.PesoKg);
    }

    [Fact]
    public async Task Tubo_redondo_1_pol_1_20_da_Sampaio_tem_4_299_kg_por_peca()
    {
        using var banco = new BancoEmMemoria();
        await using var db = await banco.CatalogoCarregadoAsync();
        var item = await db.Itens.SingleAsync(i => i.Familia == Familia.TuboRedondo && i.DiametroMm == 25.40m && i.EspessuraMm == 1.20m);

        Assert.Equal(4.299m, item.ValorPeso);
        Assert.Equal(Acabamento.FinaFrio, item.Acabamento);
        Assert.Equal(234, item.PecasPorAmarrado);
        Assert.Equal(1005.966m, CalculadoraPeso.CalcularKg(item, 234));
    }
}
