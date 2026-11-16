using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.SharedKernel.Exceptions;

namespace Mytril.MaterialId.Tests.Conferencias;

/// <summary>Tabelas aprovadas no refinamento: Análise (D17, MYT-169) e Conferência (D19, MYT-161).</summary>
public class MaquinasDeEstadoTests
{
    public static TheoryData<StatusAnalise, GatilhoAnalise, StatusAnalise> TransicoesAnalise => new()
    {
        { StatusAnalise.Capturando, GatilhoAnalise.EnviarFotos, StatusAnalise.Processando },
        { StatusAnalise.Processando, GatilhoAnalise.Concluir, StatusAnalise.AguardandoConfirmacao },
        { StatusAnalise.Processando, GatilhoAnalise.Falhar, StatusAnalise.Falha },
        { StatusAnalise.Falha, GatilhoAnalise.Reprocessar, StatusAnalise.Processando },
        { StatusAnalise.Falha, GatilhoAnalise.RefazerFotos, StatusAnalise.Capturando },
        { StatusAnalise.AguardandoConfirmacao, GatilhoAnalise.Confirmar, StatusAnalise.Confirmada },
        { StatusAnalise.AguardandoConfirmacao, GatilhoAnalise.Corrigir, StatusAnalise.Corrigida },
        { StatusAnalise.Confirmada, GatilhoAnalise.Validar, StatusAnalise.Validada },
        { StatusAnalise.Corrigida, GatilhoAnalise.Validar, StatusAnalise.Validada },
        { StatusAnalise.Confirmada, GatilhoAnalise.Devolver, StatusAnalise.AguardandoConfirmacao },
        { StatusAnalise.Corrigida, GatilhoAnalise.Devolver, StatusAnalise.AguardandoConfirmacao },
        { StatusAnalise.Capturando, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
        { StatusAnalise.Processando, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
        { StatusAnalise.Falha, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
        { StatusAnalise.AguardandoConfirmacao, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
        { StatusAnalise.Confirmada, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
        { StatusAnalise.Corrigida, GatilhoAnalise.Cancelar, StatusAnalise.Cancelada },
    };

    [Theory]
    [MemberData(nameof(TransicoesAnalise))]
    public void Analise_transicoes_validas(StatusAnalise de, GatilhoAnalise gatilho, StatusAnalise para) =>
        Assert.Equal(para, Fluxos.Analise.Proximo(de, gatilho));

    [Fact]
    public void Analise_todas_as_demais_combinacoes_sao_invalidas_e_informam_os_gatilhos_validos()
    {
        var validas = TransicoesAnalise.Select(r => ((StatusAnalise)r[0], (GatilhoAnalise)r[1])).ToHashSet();
        foreach (var status in Enum.GetValues<StatusAnalise>())
        foreach (var gatilho in Enum.GetValues<GatilhoAnalise>().Where(g => !validas.Contains((status, g))))
        {
            var ex = Assert.Throws<TransicaoInvalidaException>(() => Fluxos.Analise.Proximo(status, gatilho));
            Assert.Equal(Fluxos.Analise.GatilhosValidos(status).Select(g => g.ToString()), ex.GatilhosValidos);
        }
        Assert.Equal(validas.Count, Fluxos.Analise.Todas().Count());
    }

    [Fact]
    public void Analise_validada_e_cancelada_sao_finais()
    {
        Assert.Empty(Fluxos.Analise.GatilhosValidos(StatusAnalise.Validada));
        Assert.Empty(Fluxos.Analise.GatilhosValidos(StatusAnalise.Cancelada));
    }

    public static TheoryData<StatusConferencia, GatilhoConferencia, StatusConferencia> TransicoesConferencia => new()
    {
        { StatusConferencia.Aberta, GatilhoConferencia.Encerrar, StatusConferencia.EmValidacao },
        { StatusConferencia.EmValidacao, GatilhoConferencia.Validar, StatusConferencia.Concluida },
        { StatusConferencia.EmValidacao, GatilhoConferencia.Devolver, StatusConferencia.Aberta },
        { StatusConferencia.Aberta, GatilhoConferencia.Cancelar, StatusConferencia.Cancelada },
        { StatusConferencia.EmValidacao, GatilhoConferencia.Cancelar, StatusConferencia.Cancelada },
    };

    [Theory]
    [MemberData(nameof(TransicoesConferencia))]
    public void Conferencia_transicoes_validas(StatusConferencia de, GatilhoConferencia gatilho, StatusConferencia para) =>
        Assert.Equal(para, Fluxos.Conferencia.Proximo(de, gatilho));

    [Fact]
    public void Conferencia_todas_as_demais_combinacoes_sao_invalidas()
    {
        var validas = TransicoesConferencia.Select(r => ((StatusConferencia)r[0], (GatilhoConferencia)r[1])).ToHashSet();
        foreach (var status in Enum.GetValues<StatusConferencia>())
        foreach (var gatilho in Enum.GetValues<GatilhoConferencia>().Where(g => !validas.Contains((status, g))))
            Assert.Throws<TransicaoInvalidaException>(() => Fluxos.Conferencia.Proximo(status, gatilho));
    }
}
