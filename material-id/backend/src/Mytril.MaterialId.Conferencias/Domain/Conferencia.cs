using Mytril.MaterialId.SharedKernel.Exceptions;
using Mytril.MaterialId.SharedKernel.StateMachines;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Conferencias.Domain;

/// <summary>
/// Agregado raiz (E3-01): uma operação de Recebimento ou Expedição com um ou mais amarrados (Análises).
/// Concentra as regras que ligam a máquina da Conferência (D19) à máquina da Análise (D17).
/// </summary>
public sealed class Conferencia : IPertenceAEmpresa
{
    private static readonly StatusAnalise[] ProntasParaEncerrar = [StatusAnalise.Confirmada, StatusAnalise.Corrigida];

    private readonly List<Analise> _analises = [];
    private readonly List<Transicao> _transicoes = [];

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public Operacao Operacao { get; private set; }
    public string? Contraparte { get; private set; }
    public StatusConferencia Status { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public string CriadaPor { get; private set; } = string.Empty;

    public IReadOnlyList<Analise> Analises => _analises;
    public IReadOnlyList<Transicao> Transicoes => _transicoes;
    public IReadOnlyList<GatilhoConferencia> GatilhosValidos => Fluxos.Conferencia.GatilhosValidos(Status);

    public IEnumerable<Analise> AnalisesAtivas => _analises.Where(a => a.Ativa);
    public int QuantidadePecas => AnalisesAtivas.Sum(a => a.MedicaoAtual?.Quantidade ?? 0);
    public decimal PesoMedidoKg => AnalisesAtivas.Sum(a => a.MedicaoAtual?.PesoKg ?? 0);

    private Conferencia() { }

    public static Conferencia Abrir(Guid empresaId, Operacao operacao, string? contraparte, string usuario, DateTimeOffset agora)
    {
        if (empresaId == Guid.Empty) throw new RegraDeNegocioException("Empresa não identificada.");
        var id = Guid.NewGuid();
        var conferencia = new Conferencia
        {
            Id = id,
            EmpresaId = empresaId,
            Codigo = $"CNF-{agora:yyMMdd}-{id.ToString("N")[..5].ToUpperInvariant()}",
            Operacao = operacao,
            Contraparte = string.IsNullOrWhiteSpace(contraparte) ? null : contraparte.Trim(),
            Status = StatusConferencia.Aberta,
            CriadaEm = agora,
            CriadaPor = usuario
        };
        conferencia._transicoes.Add(new Transicao("—", StatusConferencia.Aberta.ToString(), "Abrir", usuario, agora, null));
        return conferencia;
    }

    // ---------- Conferência ----------

    /// <summary>O operador encerra quando todos os amarrados ativos foram confirmados ou corrigidos.</summary>
    public void Encerrar(string usuario, DateTimeOffset agora)
    {
        ExigirTransicao(GatilhoConferencia.Encerrar);
        if (!AnalisesAtivas.Any()) throw new RegraDeNegocioException("Adicione pelo menos um amarrado antes de encerrar.");
        if (AnalisesAtivas.Any(a => !ProntasParaEncerrar.Contains(a.Status)))
            throw new RegraDeNegocioException("Todos os amarrados precisam estar confirmados ou corrigidos para encerrar.");
        Disparar(GatilhoConferencia.Encerrar, usuario, agora);
    }

    /// <summary>O gestor conclui quando todos os amarrados ativos estão validados. Peso fica travado.</summary>
    public void Validar(string usuario, DateTimeOffset agora)
    {
        ExigirTransicao(GatilhoConferencia.Validar);
        if (AnalisesAtivas.Any(a => a.Status != StatusAnalise.Validada))
            throw new RegraDeNegocioException("Valide todos os amarrados antes de concluir a conferência.");
        Disparar(GatilhoConferencia.Validar, usuario, agora);
    }

    public void Devolver(string motivo, string usuario, DateTimeOffset agora) =>
        Disparar(GatilhoConferencia.Devolver, usuario, agora, Analise.MotivoObrigatorio(motivo, "Informe o motivo da devolução."));

    /// <summary>Cancelar a conferência cancela também os amarrados que ainda não foram validados.</summary>
    public void Cancelar(string motivo, string usuario, DateTimeOffset agora)
    {
        var m = Analise.MotivoObrigatorio(motivo, "Informe o motivo do cancelamento.");
        Disparar(GatilhoConferencia.Cancelar, usuario, agora, m);
        foreach (var analise in _analises.Where(a => !Fluxos.Analise.EhFinal(a.Status)))
            analise.Cancelar($"Conferência cancelada: {m}", usuario, agora);
    }

    // ---------- Análises (amarrados) ----------

    public Analise AdicionarAnalise(string usuario, DateTimeOffset agora)
    {
        ExigirAberta();
        var analise = new Analise(EmpresaId, Id, _analises.Count + 1, usuario, agora);
        _analises.Add(analise);
        return analise;
    }

    public void EnviarFotos(Guid analiseId, string usuario, DateTimeOffset agora) { ExigirAberta(); ObterAnalise(analiseId).EnviarFotos(usuario, agora); }

    public void ConcluirProcessamento(Guid analiseId, Medicao resultado, decimal confianca, string versaoModelo, string usuario, DateTimeOffset agora)
    { ExigirAberta(); ObterAnalise(analiseId).Concluir(resultado, confianca, versaoModelo, usuario, agora); }

    public void FalharProcessamento(Guid analiseId, string motivo, string usuario, DateTimeOffset agora)
    { ExigirAberta(); ObterAnalise(analiseId).Falhar(motivo, usuario, agora); }

    public void Reprocessar(Guid analiseId, string usuario, DateTimeOffset agora) { ExigirAberta(); ObterAnalise(analiseId).Reprocessar(usuario, agora); }

    public void RefazerFotos(Guid analiseId, string usuario, DateTimeOffset agora) { ExigirAberta(); ObterAnalise(analiseId).RefazerFotos(usuario, agora); }

    public void Confirmar(Guid analiseId, string usuario, DateTimeOffset agora) { ExigirAberta(); ObterAnalise(analiseId).Confirmar(usuario, agora); }

    public void Corrigir(Guid analiseId, Medicao valorCorreto, string usuario, DateTimeOffset agora)
    { ExigirAberta(); ObterAnalise(analiseId).Corrigir(valorCorreto, usuario, agora); }

    public void CancelarAnalise(Guid analiseId, string motivo, string usuario, DateTimeOffset agora)
    {
        if (Fluxos.Conferencia.EhFinal(Status)) throw new RegraDeNegocioException($"A conferência está {Status} e não aceita alterações.");
        ObterAnalise(analiseId).Cancelar(motivo, usuario, agora);
    }

    /// <summary>O gestor valida um amarrado durante a validação da conferência.</summary>
    public void ValidarAnalise(Guid analiseId, string usuario, DateTimeOffset agora)
    {
        if (Status != StatusConferencia.EmValidacao) throw new RegraDeNegocioException("Só é possível validar amarrados de uma conferência em validação.");
        ObterAnalise(analiseId).Validar(usuario, agora);
    }

    /// <summary>
    /// O gestor devolve um amarrado ao operador. Como o operador só altera conferências abertas,
    /// a conferência em validação volta para Aberta com o mesmo motivo.
    /// </summary>
    public void DevolverAnalise(Guid analiseId, string motivo, string usuario, DateTimeOffset agora)
    {
        if (Status != StatusConferencia.EmValidacao) throw new RegraDeNegocioException("Só é possível devolver amarrados de uma conferência em validação.");
        var m = Analise.MotivoObrigatorio(motivo, "Informe o motivo da devolução.");
        ObterAnalise(analiseId).Devolver(m, usuario, agora);
        Disparar(GatilhoConferencia.Devolver, usuario, agora, $"Amarrado devolvido: {m}");
    }

    public Analise ObterAnalise(Guid analiseId) =>
        _analises.FirstOrDefault(a => a.Id == analiseId) ?? throw new NaoEncontradoException($"Amarrado {analiseId} não encontrado nesta conferência.");

    private void ExigirAberta()
    {
        if (Status != StatusConferencia.Aberta)
            throw new RegraDeNegocioException($"A conferência está {Status}. Só conferências abertas aceitam alterações nos amarrados.");
    }

    private void ExigirTransicao(GatilhoConferencia gatilho) => Fluxos.Conferencia.Proximo(Status, gatilho);

    private void Disparar(GatilhoConferencia gatilho, string usuario, DateTimeOffset agora, string? motivo = null)
    {
        var proximo = Fluxos.Conferencia.Proximo(Status, gatilho);
        _transicoes.Add(new Transicao(Status.ToString(), proximo.ToString(), gatilho.ToString(), usuario, agora, motivo));
        Status = proximo;
    }
}
