namespace Mytril.MaterialId.SharedKernel.StateMachines;

/// <summary>Registro imutável de uma transição de status (E1-05). Gravado na base e publicado no Mytril Audit.</summary>
public sealed class Transicao
{
    public Guid Id { get; private set; }
    public string De { get; private set; } = string.Empty;
    public string Para { get; private set; } = string.Empty;
    public string Gatilho { get; private set; } = string.Empty;
    public string Usuario { get; private set; } = string.Empty;
    public DateTimeOffset Em { get; private set; }
    public string? Motivo { get; private set; }

    private Transicao() { }

    public Transicao(string de, string para, string gatilho, string usuario, DateTimeOffset em, string? motivo)
    {
        Id = Guid.NewGuid();
        De = de;
        Para = para;
        Gatilho = gatilho;
        Usuario = usuario;
        Em = em;
        Motivo = motivo;
    }
}
