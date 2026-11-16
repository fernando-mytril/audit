namespace Mytril.MaterialId.SharedKernel.Exceptions;

/// <summary>Gatilho não permitido no status atual. A API responde 409 com os gatilhos válidos.</summary>
public sealed class TransicaoInvalidaException(string statusAtual, string gatilho, IReadOnlyList<string> gatilhosValidos)
    : RegraDeNegocioException($"O gatilho '{gatilho}' não é permitido no status '{statusAtual}'.")
{
    public string StatusAtual { get; } = statusAtual;
    public string Gatilho { get; } = gatilho;
    public IReadOnlyList<string> GatilhosValidos { get; } = gatilhosValidos;
}
