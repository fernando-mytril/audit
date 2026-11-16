using Mytril.MaterialId.Catalogo.Domain;
using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Tests.Catalogo;

/// <summary>Critérios de aceite do E4-03 (MYT-168), com valores reais das tabelas dos fabricantes.</summary>
public class CalculadoraPesoTests
{
    private static ItemCatalogo Item(TipoPeso tipo, decimal valor, decimal? comprimentoPadrao, Familia familia = Familia.TuboRetangular) =>
        new(Guid.NewGuid(), Guid.NewGuid(), familia, Acabamento.FinaFrio, 1.20m, null, 40, 20, null, null, tipo, valor, comprimentoPadrao, null, false, null);

    [Fact]
    public void Tubo_retangular_40x20_esp_1_20_massa_linear_98_pecas_da_642_1_kg()
    {
        var item = Item(TipoPeso.KgPorMetro, 1.092m, 6000);
        Assert.Equal(642.096m, CalculadoraPeso.CalcularKg(item, 98));
        Assert.Equal(642.1m, Math.Round(CalculadoraPeso.CalcularKg(item, 98), 1));
    }

    [Fact]
    public void Tubo_redondo_1_pol_esp_1_20_kg_por_peca_234_pecas_da_1006_0_kg()
    {
        var item = Item(TipoPeso.KgPorPeca, 4.299m, 6000, Familia.TuboRedondo);
        Assert.Equal(1005.966m, CalculadoraPeso.CalcularKg(item, 234));
        Assert.Equal(1006.0m, Math.Round(CalculadoraPeso.CalcularKg(item, 234), 1));
    }

    [Fact]
    public void Chapa_bitola_14_1200x3000_10_chapas_da_549_3_kg()
    {
        var item = Item(TipoPeso.KgPorMetroQuadrado, 15.258m, null, Familia.Chapa);
        Assert.Equal(549.288m, CalculadoraPeso.CalcularKg(item, 10, comprimentoMm: 3000, larguraMm: 1200));
    }

    [Fact]
    public void Peca_com_comprimento_diferente_do_padrao_e_proporcional()
    {
        var item = Item(TipoPeso.KgPorPeca, 6.000m, 6000);
        Assert.Equal(58.000m, CalculadoraPeso.CalcularKg(item, 10, comprimentoMm: 5800));
    }

    [Fact]
    public void Chapa_sem_largura_exige_a_medida()
    {
        var item = Item(TipoPeso.KgPorMetroQuadrado, 15.258m, null, Familia.Chapa);
        var ex = Assert.Throws<RegraDeNegocioException>(() => CalculadoraPeso.CalcularKg(item, 10, comprimentoMm: 3000));
        Assert.Contains("largura", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantidade_deve_ser_positiva(int quantidade) =>
        Assert.Throws<RegraDeNegocioException>(() => CalculadoraPeso.CalcularKg(Item(TipoPeso.KgPorMetro, 1m, 6000), quantidade));
}
