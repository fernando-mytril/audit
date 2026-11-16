using Mytril.MaterialId.Catalogo.Domain;

namespace Mytril.MaterialId.Catalogo.Application;

public sealed record FabricanteDto(Guid Id, string Nome, string? Fonte, string? Observacao);

public sealed record ItemCatalogoDto(
    Guid Id, Guid FabricanteId, string Fabricante, Familia Familia, Acabamento Acabamento, string Descricao,
    decimal EspessuraMm, decimal? DiametroMm, decimal? LarguraMm, decimal? AlturaMm, decimal? AbaMm, string? Bitola,
    TipoPeso TipoPeso, decimal ValorPeso, decimal? ComprimentoPadraoMm, int? PecasPorAmarrado,
    bool RequerRevisao, string? Observacao);

public sealed record FiltroCatalogo(Guid? FabricanteId = null, Familia? Familia = null, string? Busca = null, bool? RequerRevisao = null);

public sealed record CalculoPesoDto(Guid ItemId, string Descricao, int Quantidade, decimal? ComprimentoMm, decimal? LarguraMm, decimal PesoKg);

/// <summary>API pública do módulo Catálogo para os demais módulos (Conferências, IA, Dashboard).</summary>
public interface ICatalogoConsulta
{
    Task<IReadOnlyList<FabricanteDto>> ListarFabricantesAsync(CancellationToken ct);
    Task<IReadOnlyList<ItemCatalogoDto>> ListarItensAsync(FiltroCatalogo filtro, CancellationToken ct);
    Task<ItemCatalogoDto?> ObterItemAsync(Guid id, CancellationToken ct);
    Task<CalculoPesoDto> CalcularPesoAsync(Guid itemId, int quantidade, decimal? comprimentoMm, decimal? larguraMm, CancellationToken ct);
}
