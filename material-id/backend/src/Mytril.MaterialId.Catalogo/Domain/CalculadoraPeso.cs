using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Catalogo.Domain;

/// <summary>
/// Peso pela tabela do fabricante (E4-03).
/// Peça: kg/peça × quantidade, proporcional ao comprimento medido quando diferente do padrão.
/// Metro: kg/m × comprimento × quantidade. Metro quadrado: kg/m² × largura × comprimento × quantidade.
/// </summary>
public static class CalculadoraPeso
{
    public const int CasasGravadas = 3;

    public static decimal CalcularKg(ItemCatalogo item, int quantidade, decimal? comprimentoMm = null, decimal? larguraMm = null)
    {
        if (quantidade <= 0) throw new RegraDeNegocioException("A quantidade deve ser maior que zero.");
        if (comprimentoMm is <= 0) throw new RegraDeNegocioException("O comprimento deve ser maior que zero.");
        if (larguraMm is <= 0) throw new RegraDeNegocioException("A largura deve ser maior que zero.");

        var kg = item.TipoPeso switch
        {
            TipoPeso.KgPorPeca => PorPeca(item, quantidade, comprimentoMm),
            TipoPeso.KgPorMetro => item.ValorPeso * Metros(comprimentoMm ?? item.ComprimentoPadraoMm, "comprimento") * quantidade,
            TipoPeso.KgPorMetroQuadrado => item.ValorPeso * Metros(larguraMm, "largura") * Metros(comprimentoMm, "comprimento") * quantidade,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.TipoPeso, "Tipo de peso desconhecido.")
        };

        return Math.Round(kg, CasasGravadas, MidpointRounding.AwayFromZero);
    }

    private static decimal PorPeca(ItemCatalogo item, int quantidade, decimal? comprimentoMm)
    {
        var padrao = item.ComprimentoPadraoMm!.Value;
        var comprimento = comprimentoMm ?? padrao;
        return item.ValorPeso / padrao * comprimento * quantidade;
    }

    private static decimal Metros(decimal? mm, string campo) =>
        mm is { } valor ? valor / 1000m : throw new RegraDeNegocioException($"Informe a {campo} para calcular o peso deste item.");
}
