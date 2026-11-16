namespace Mytril.MaterialId.Api.Seguranca;

/// <summary>
/// Contexto de execução fora de uma requisição HTTP (ex.: processamento em segundo plano).
/// Quando preenchido, tem prioridade sobre o usuário do token.
/// </summary>
public sealed class ContextoExecucao
{
    public Guid? EmpresaId { get; private set; }
    public string? Usuario { get; private set; }

    public void Definir(Guid empresaId, string usuario)
    {
        EmpresaId = empresaId;
        Usuario = usuario;
    }
}
