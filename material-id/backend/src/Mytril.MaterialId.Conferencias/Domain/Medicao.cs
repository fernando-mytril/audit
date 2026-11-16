using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Conferencias.Domain;

/// <summary>Produto do catálogo, quantidade, dimensões medidas e peso calculado pela tabela do fabricante.</summary>
public sealed record Medicao
{
    public Guid ItemCatalogoId { get; init; }
    public int Quantidade { get; init; }
    public decimal? ComprimentoMm { get; init; }
    public decimal? LarguraMm { get; init; }
    public decimal PesoKg { get; init; }

    private Medicao() { }

    public static Medicao Criar(Guid itemCatalogoId, int quantidade, decimal? comprimentoMm, decimal? larguraMm, decimal pesoKg)
    {
        if (itemCatalogoId == Guid.Empty) throw new RegraDeNegocioException("Informe o produto do catálogo.");
        if (quantidade <= 0) throw new RegraDeNegocioException("A quantidade deve ser maior que zero.");
        if (comprimentoMm is <= 0) throw new RegraDeNegocioException("O comprimento deve ser maior que zero.");
        if (larguraMm is <= 0) throw new RegraDeNegocioException("A largura deve ser maior que zero.");
        if (pesoKg <= 0) throw new RegraDeNegocioException("O peso deve ser maior que zero.");
        return new Medicao { ItemCatalogoId = itemCatalogoId, Quantidade = quantidade, ComprimentoMm = comprimentoMm, LarguraMm = larguraMm, PesoKg = pesoKg };
    }

    /// <summary>Mesmos produto, quantidade e dimensões (o peso é consequência deles).</summary>
    public bool MesmosValores(Medicao outra) =>
        ItemCatalogoId == outra.ItemCatalogoId && Quantidade == outra.Quantidade &&
        ComprimentoMm == outra.ComprimentoMm && LarguraMm == outra.LarguraMm;
}
