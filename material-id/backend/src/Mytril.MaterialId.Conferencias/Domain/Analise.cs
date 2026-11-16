using Mytril.MaterialId.SharedKernel.Exceptions;
using Mytril.MaterialId.SharedKernel.StateMachines;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Conferencias.Domain;

/// <summary>
/// Análise de um amarrado ou pilha (E4-04). Só é alterada pela <see cref="Conferencia"/>,
/// que garante as regras entre as duas máquinas de estado.
/// </summary>
public sealed class Analise : IPertenceAEmpresa
{
    private readonly List<Transicao> _transicoes = [];

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid ConferenciaId { get; private set; }
    public int Sequencia { get; private set; }
    public StatusAnalise Status { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>O que o modelo de IA identificou.</summary>
    public Medicao? Resultado { get; private set; }
    public decimal? Confianca { get; private set; }
    public string? VersaoModelo { get; private set; }

    /// <summary>Valor correto informado pelo operador ao corrigir (D6).</summary>
    public Medicao? Correcao { get; private set; }
    public string? MotivoFalha { get; private set; }

    public IReadOnlyList<Transicao> Transicoes => _transicoes;
    public Medicao? MedicaoAtual => Correcao ?? Resultado;
    public bool Ativa => Status != StatusAnalise.Cancelada;
    public IReadOnlyList<GatilhoAnalise> GatilhosValidos => Fluxos.Analise.GatilhosValidos(Status);

    private Analise() { }

    internal Analise(Guid empresaId, Guid conferenciaId, int sequencia, string usuario, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        ConferenciaId = conferenciaId;
        Sequencia = sequencia;
        Status = StatusAnalise.Capturando;
        CriadaEm = agora;
        _transicoes.Add(new Transicao("—", Status.ToString(), "IniciarCaptura", usuario, agora, null));
    }

    internal void EnviarFotos(string usuario, DateTimeOffset agora) => Disparar(GatilhoAnalise.EnviarFotos, usuario, agora);

    internal void Concluir(Medicao resultado, decimal confianca, string versaoModelo, string usuario, DateTimeOffset agora)
    {
        if (confianca is < 0 or > 1) throw new RegraDeNegocioException("A confiança deve estar entre 0 e 1.");
        Disparar(GatilhoAnalise.Concluir, usuario, agora);
        Resultado = resultado;
        Confianca = confianca;
        VersaoModelo = versaoModelo;
        Correcao = null;
        MotivoFalha = null;
    }

    internal void Falhar(string motivo, string usuario, DateTimeOffset agora)
    {
        var m = MotivoObrigatorio(motivo, "Informe o motivo da falha.");
        Disparar(GatilhoAnalise.Falhar, usuario, agora, m);
        MotivoFalha = m;
    }

    internal void Reprocessar(string usuario, DateTimeOffset agora) => Disparar(GatilhoAnalise.Reprocessar, usuario, agora);

    internal void RefazerFotos(string usuario, DateTimeOffset agora) => Disparar(GatilhoAnalise.RefazerFotos, usuario, agora);

    internal void Confirmar(string usuario, DateTimeOffset agora)
    {
        Disparar(GatilhoAnalise.Confirmar, usuario, agora);
        Correcao = null;
    }

    /// <summary>Negar exige o valor correto (D6). Corrigir sem mudar nada é bloqueado.</summary>
    internal void Corrigir(Medicao valorCorreto, string usuario, DateTimeOffset agora)
    {
        if (Resultado is not null && Resultado.MesmosValores(valorCorreto))
            throw new RegraDeNegocioException("Altere pelo menos um valor ou use Confirmar.");
        Disparar(GatilhoAnalise.Corrigir, usuario, agora);
        Correcao = valorCorreto;
    }

    internal void Validar(string usuario, DateTimeOffset agora) => Disparar(GatilhoAnalise.Validar, usuario, agora);

    internal void Devolver(string motivo, string usuario, DateTimeOffset agora) =>
        Disparar(GatilhoAnalise.Devolver, usuario, agora, MotivoObrigatorio(motivo, "Informe o motivo da devolução."));

    internal void Cancelar(string motivo, string usuario, DateTimeOffset agora) =>
        Disparar(GatilhoAnalise.Cancelar, usuario, agora, MotivoObrigatorio(motivo, "Informe o motivo do cancelamento."));

    private void Disparar(GatilhoAnalise gatilho, string usuario, DateTimeOffset agora, string? motivo = null)
    {
        var proximo = Fluxos.Analise.Proximo(Status, gatilho);
        _transicoes.Add(new Transicao(Status.ToString(), proximo.ToString(), gatilho.ToString(), usuario, agora, motivo));
        Status = proximo;
    }

    internal static string MotivoObrigatorio(string? motivo, string mensagem) =>
        string.IsNullOrWhiteSpace(motivo) ? throw new RegraDeNegocioException(mensagem) : motivo.Trim();
}
