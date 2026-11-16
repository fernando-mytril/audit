using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Tests.Conferencias;

/// <summary>Regras que ligam a Conferência às Análises (E3-01, E4-04, E5-01, E5-02).</summary>
public class ConferenciaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid OutroItem = Guid.NewGuid();

    private static Medicao M(int qtd = 98, decimal peso = 642.096m, Guid? item = null) => Medicao.Criar(item ?? Item, qtd, 6000, null, peso);

    private static (Conferencia, Analise) ComAnaliseAguardando()
    {
        var c = Conferencia.Abrir(Guid.NewGuid(), Operacao.Recebimento, "Aço Cearense", "joao", Agora);
        var a = c.AdicionarAnalise("joao", Agora);
        c.EnviarFotos(a.Id, "joao", Agora);
        c.ConcluirProcessamento(a.Id, M(), 0.87m, "beta-0", "sistema:ia", Agora);
        return (c, a);
    }

    [Fact]
    public void Fluxo_completo_ate_concluida_soma_peso_e_registra_linha_do_tempo()
    {
        var (c, a) = ComAnaliseAguardando();
        var b = c.AdicionarAnalise("joao", Agora);
        c.EnviarFotos(b.Id, "joao", Agora);
        c.ConcluirProcessamento(b.Id, M(96, 600m), 0.55m, "beta-0", "sistema:ia", Agora);

        c.Confirmar(a.Id, "joao", Agora);
        c.Corrigir(b.Id, M(98, 642.096m), "joao", Agora);
        c.Encerrar("joao", Agora);
        c.ValidarAnalise(a.Id, "maria", Agora);
        c.ValidarAnalise(b.Id, "maria", Agora);
        c.Validar("maria", Agora);

        Assert.Equal(StatusConferencia.Concluida, c.Status);
        Assert.Equal(196, c.QuantidadePecas);
        Assert.Equal(1284.192m, c.PesoMedidoKg);
        Assert.Equal(["Abrir", "Encerrar", "Validar"], c.Transicoes.Select(t => t.Gatilho));
        Assert.Equal("maria", c.Transicoes.Last().Usuario);
        Assert.Equal(["IniciarCaptura", "EnviarFotos", "Concluir", "Corrigir", "Validar"], b.Transicoes.Select(t => t.Gatilho));
        Assert.Empty(c.GatilhosValidos);
    }

    [Fact]
    public void Nao_encerra_sem_amarrados()
    {
        var c = Conferencia.Abrir(Guid.NewGuid(), Operacao.Expedicao, null, "joao", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.Encerrar("joao", Agora));
    }

    [Fact]
    public void Nao_encerra_com_amarrado_aguardando_confirmacao()
    {
        var (c, _) = ComAnaliseAguardando();
        var ex = Assert.Throws<RegraDeNegocioException>(() => c.Encerrar("joao", Agora));
        Assert.Contains("confirmados ou corrigidos", ex.Message);
    }

    [Fact]
    public void Amarrado_cancelado_nao_impede_encerrar_nem_soma_peso()
    {
        var (c, a) = ComAnaliseAguardando();
        var b = c.AdicionarAnalise("joao", Agora);
        c.CancelarAnalise(b.Id, "Foto do amarrado errado", "joao", Agora);
        c.Confirmar(a.Id, "joao", Agora);
        c.Encerrar("joao", Agora);
        Assert.Equal(StatusConferencia.EmValidacao, c.Status);
        Assert.Equal(642.096m, c.PesoMedidoKg);
    }

    [Fact]
    public void Corrigir_exige_ao_menos_um_valor_diferente()
    {
        var (c, a) = ComAnaliseAguardando();
        var ex = Assert.Throws<RegraDeNegocioException>(() => c.Corrigir(a.Id, M(), "joao", Agora));
        Assert.Equal("Altere pelo menos um valor ou use Confirmar.", ex.Message);
        Assert.Equal(StatusAnalise.AguardandoConfirmacao, a.Status);
    }

    [Fact]
    public void Corrigir_o_produto_guarda_estimado_e_correto_lado_a_lado()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Corrigir(a.Id, M(item: OutroItem, peso: 700m), "joao", Agora);
        Assert.Equal(Item, a.Resultado!.ItemCatalogoId);
        Assert.Equal(OutroItem, a.Correcao!.ItemCatalogoId);
        Assert.Equal(700m, a.MedicaoAtual!.PesoKg);
    }

    [Fact]
    public void Operador_nao_altera_amarrado_de_conferencia_em_validacao()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Confirmar(a.Id, "joao", Agora);
        c.Encerrar("joao", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.AdicionarAnalise("joao", Agora));
        Assert.Throws<RegraDeNegocioException>(() => c.Corrigir(a.Id, M(97), "joao", Agora));
    }

    [Fact]
    public void Gestor_so_valida_amarrado_com_conferencia_em_validacao()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Confirmar(a.Id, "joao", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.ValidarAnalise(a.Id, "maria", Agora));
    }

    [Fact]
    public void Nao_conclui_sem_validar_todos_os_amarrados()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Confirmar(a.Id, "joao", Agora);
        c.Encerrar("joao", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.Validar("maria", Agora));
    }

    [Fact]
    public void Devolver_amarrado_reabre_a_conferencia_com_o_motivo()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Confirmar(a.Id, "joao", Agora);
        c.Encerrar("joao", Agora);

        c.DevolverAnalise(a.Id, "Contagem não bate com a foto", "maria", Agora);

        Assert.Equal(StatusAnalise.AguardandoConfirmacao, a.Status);
        Assert.Equal(StatusConferencia.Aberta, c.Status);
        Assert.Equal("Amarrado devolvido: Contagem não bate com a foto", c.Transicoes.Last().Motivo);
        c.Corrigir(a.Id, M(97, 635.544m), "joao", Agora);
        Assert.Equal(StatusAnalise.Corrigida, a.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Devolver_e_cancelar_exigem_motivo(string motivo)
    {
        var (c, a) = ComAnaliseAguardando();
        c.Confirmar(a.Id, "joao", Agora);
        c.Encerrar("joao", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.Devolver(motivo, "maria", Agora));
        Assert.Throws<RegraDeNegocioException>(() => c.Cancelar(motivo, "maria", Agora));
    }

    [Fact]
    public void Cancelar_conferencia_cancela_os_amarrados_pendentes()
    {
        var (c, a) = ComAnaliseAguardando();
        c.Cancelar("Carga devolvida ao fornecedor", "joao", Agora);
        Assert.Equal(StatusConferencia.Cancelada, c.Status);
        Assert.Equal(StatusAnalise.Cancelada, a.Status);
        Assert.Throws<RegraDeNegocioException>(() => c.AdicionarAnalise("joao", Agora));
    }

    [Fact]
    public void Falha_guarda_motivo_e_permite_refazer_as_fotos()
    {
        var c = Conferencia.Abrir(Guid.NewGuid(), Operacao.Recebimento, null, "joao", Agora);
        var a = c.AdicionarAnalise("joao", Agora);
        c.EnviarFotos(a.Id, "joao", Agora);
        c.FalharProcessamento(a.Id, "Foto escura", "sistema:ia", Agora);
        Assert.Equal("Foto escura", a.MotivoFalha);
        c.RefazerFotos(a.Id, "joao", Agora);
        Assert.Equal(StatusAnalise.Capturando, a.Status);
    }
}
