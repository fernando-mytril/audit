using System.Globalization;

namespace Mytril.MaterialId.Catalogo.Domain;

/// <summary>
/// Uma linha da tabela técnica de um fabricante (E2-01).
/// Dimensões em mm. O significado de Largura/Altura/Aba depende da família:
/// tubo retangular e quadrado = base × altura; perfil = alma × aba (× enrijecedor); chapa = sem dimensão fixa.
/// </summary>
public sealed class ItemCatalogo
{
    public Guid Id { get; private set; }
    public Guid FabricanteId { get; private set; }
    public Familia Familia { get; private set; }
    public Acabamento Acabamento { get; private set; }
    public decimal EspessuraMm { get; private set; }
    public decimal? DiametroMm { get; private set; }
    public decimal? LarguraMm { get; private set; }
    public decimal? AlturaMm { get; private set; }
    public decimal? AbaMm { get; private set; }
    public string? Bitola { get; private set; }
    public TipoPeso TipoPeso { get; private set; }
    public decimal ValorPeso { get; private set; }
    public decimal? ComprimentoPadraoMm { get; private set; }
    public int? PecasPorAmarrado { get; private set; }
    public bool Ativo { get; private set; } = true;
    public bool RequerRevisao { get; private set; }
    public string? Observacao { get; private set; }
    public string Descricao { get; private set; } = string.Empty;

    private ItemCatalogo() { }

    public ItemCatalogo(Guid id, Guid fabricanteId, Familia familia, Acabamento acabamento, decimal espessuraMm,
        decimal? diametroMm, decimal? larguraMm, decimal? alturaMm, decimal? abaMm, string? bitola,
        TipoPeso tipoPeso, decimal valorPeso, decimal? comprimentoPadraoMm, int? pecasPorAmarrado,
        bool requerRevisao, string? observacao)
    {
        if (espessuraMm <= 0) throw new ArgumentException("Espessura deve ser maior que zero.", nameof(espessuraMm));
        if (valorPeso <= 0) throw new ArgumentException("Peso deve ser maior que zero.", nameof(valorPeso));
        if (tipoPeso == TipoPeso.KgPorPeca && comprimentoPadraoMm is null)
            throw new ArgumentException("Peso por peça exige o comprimento padrão da peça.", nameof(comprimentoPadraoMm));

        Id = id;
        FabricanteId = fabricanteId;
        Familia = familia;
        Acabamento = acabamento;
        EspessuraMm = espessuraMm;
        DiametroMm = diametroMm;
        LarguraMm = larguraMm;
        AlturaMm = alturaMm;
        AbaMm = abaMm;
        Bitola = bitola;
        TipoPeso = tipoPeso;
        ValorPeso = valorPeso;
        ComprimentoPadraoMm = comprimentoPadraoMm;
        PecasPorAmarrado = pecasPorAmarrado;
        RequerRevisao = requerRevisao;
        Observacao = observacao;
        Descricao = MontarDescricao();
    }

    public void Desativar() => Ativo = false;

    private string MontarDescricao()
    {
        static string N(decimal v) => v.ToString(v % 1 == 0 ? "0" : "0.00", CultureInfo.GetCultureInfo("pt-BR"));
        var medida = Familia switch
        {
            Familia.TuboRedondo => $"Tubo redondo Ø {N(DiametroMm ?? 0)}{(Bitola is null ? "" : $" ({Bitola})")}",
            Familia.TuboQuadrado => $"Tubo quadrado {N(LarguraMm ?? 0)}×{N(AlturaMm ?? 0)}",
            Familia.TuboRetangular => $"Tubo retangular {N(LarguraMm ?? 0)}×{N(AlturaMm ?? 0)}",
            Familia.PerfilUSimples => $"Perfil U {N(AlturaMm ?? 0)}×{N(LarguraMm ?? 0)}×{N(AlturaMm ?? 0)}",
            Familia.PerfilUEnrijecido => $"Perfil U enrijecido {N(AbaMm ?? 0)}×{N(AlturaMm ?? 0)}×{N(LarguraMm ?? 0)}",
            Familia.Chapa => $"Chapa{(Bitola is null ? "" : $" bitola {Bitola}")}",
            _ => Familia.ToString()
        };
        var acabamento = Acabamento switch
        {
            Acabamento.FinaFrio => "fina frio",
            Acabamento.Galvanizado => "galvanizado",
            Acabamento.FinaQuente => "fina quente",
            Acabamento.Galvalume => "galvalume",
            _ => Acabamento.ToString()
        };
        return $"{medida} esp. {N(EspessuraMm)} mm {acabamento}";
    }
}
