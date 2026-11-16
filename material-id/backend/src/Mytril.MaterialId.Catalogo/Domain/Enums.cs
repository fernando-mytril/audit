namespace Mytril.MaterialId.Catalogo.Domain;

public enum Familia
{
    TuboRedondo,
    TuboQuadrado,
    TuboRetangular,
    PerfilUSimples,
    PerfilUEnrijecido,
    Chapa
}

/// <summary>Acabamento do aço. FF/LF = fina frio, ZC/GA = galvanizado, FQ/LQ = fina quente.</summary>
public enum Acabamento
{
    FinaFrio,
    Galvanizado,
    FinaQuente,
    Galvalume
}

/// <summary>Como o fabricante publica o peso do item.</summary>
public enum TipoPeso
{
    KgPorPeca,
    KgPorMetro,
    KgPorMetroQuadrado
}
