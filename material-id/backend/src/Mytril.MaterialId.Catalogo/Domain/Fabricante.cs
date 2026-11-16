namespace Mytril.MaterialId.Catalogo.Domain;

/// <summary>Fabricante cuja tabela técnica compõe o catálogo da plataforma (D26).</summary>
public sealed class Fabricante
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Fonte { get; private set; }
    public string? Observacao { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Fabricante() { }

    public Fabricante(Guid id, string nome, string? fonte, string? observacao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do fabricante é obrigatório.", nameof(nome));
        Id = id;
        Nome = nome.Trim();
        Fonte = fonte;
        Observacao = observacao;
    }
}
