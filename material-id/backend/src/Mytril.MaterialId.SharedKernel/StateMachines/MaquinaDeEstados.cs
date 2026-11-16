using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.SharedKernel.StateMachines;

/// <summary>
/// Máquina de estados declarativa: a tabela de transições é a única fonte de verdade do fluxo.
/// Usada pelas máquinas da Análise, da Conferência e da Referência.
/// </summary>
public sealed class MaquinaDeEstados<TStatus, TGatilho>
    where TStatus : struct, Enum
    where TGatilho : struct, Enum
{
    private readonly Dictionary<(TStatus, TGatilho), TStatus> _transicoes = new();
    private readonly HashSet<TStatus> _finais = [];

    public MaquinaDeEstados<TStatus, TGatilho> Permitir(TStatus de, TGatilho gatilho, TStatus para)
    {
        if (!_transicoes.TryAdd((de, gatilho), para))
            throw new InvalidOperationException($"Transição duplicada: {de} + {gatilho}.");
        return this;
    }

    public MaquinaDeEstados<TStatus, TGatilho> Permitir(IEnumerable<TStatus> de, TGatilho gatilho, TStatus para)
    {
        foreach (var status in de) Permitir(status, gatilho, para);
        return this;
    }

    public MaquinaDeEstados<TStatus, TGatilho> Finais(params TStatus[] finais)
    {
        _finais.UnionWith(finais);
        return this;
    }

    public bool EhFinal(TStatus status) => _finais.Contains(status);

    public bool PodeDisparar(TStatus atual, TGatilho gatilho) => _transicoes.ContainsKey((atual, gatilho));

    public IReadOnlyList<TGatilho> GatilhosValidos(TStatus atual) =>
        _transicoes.Keys.Where(k => k.Item1.Equals(atual)).Select(k => k.Item2).OrderBy(g => g).ToList();

    public TStatus Proximo(TStatus atual, TGatilho gatilho) =>
        _transicoes.TryGetValue((atual, gatilho), out var proximo)
            ? proximo
            : throw new TransicaoInvalidaException(atual.ToString(), gatilho.ToString(),
                GatilhosValidos(atual).Select(g => g.ToString()).ToList());

    public IEnumerable<(TStatus De, TGatilho Gatilho, TStatus Para)> Todas() =>
        _transicoes.Select(t => (t.Key.Item1, t.Key.Item2, t.Value));
}
