using Mytril.MaterialId.SharedKernel.StateMachines;

namespace Mytril.MaterialId.Conferencias.Domain;

/// <summary>Tabelas de transição aprovadas no refinamento (D17 e D19).</summary>
public static class Fluxos
{
    public static readonly MaquinaDeEstados<StatusAnalise, GatilhoAnalise> Analise = new MaquinaDeEstados<StatusAnalise, GatilhoAnalise>()
        .Permitir(StatusAnalise.Capturando, GatilhoAnalise.EnviarFotos, StatusAnalise.Processando)
        .Permitir(StatusAnalise.Processando, GatilhoAnalise.Concluir, StatusAnalise.AguardandoConfirmacao)
        .Permitir(StatusAnalise.Processando, GatilhoAnalise.Falhar, StatusAnalise.Falha)
        .Permitir(StatusAnalise.Falha, GatilhoAnalise.Reprocessar, StatusAnalise.Processando)
        .Permitir(StatusAnalise.Falha, GatilhoAnalise.RefazerFotos, StatusAnalise.Capturando)
        .Permitir(StatusAnalise.AguardandoConfirmacao, GatilhoAnalise.Confirmar, StatusAnalise.Confirmada)
        .Permitir(StatusAnalise.AguardandoConfirmacao, GatilhoAnalise.Corrigir, StatusAnalise.Corrigida)
        .Permitir([StatusAnalise.Confirmada, StatusAnalise.Corrigida], GatilhoAnalise.Validar, StatusAnalise.Validada)
        .Permitir([StatusAnalise.Confirmada, StatusAnalise.Corrigida], GatilhoAnalise.Devolver, StatusAnalise.AguardandoConfirmacao)
        .Permitir([StatusAnalise.Capturando, StatusAnalise.Processando, StatusAnalise.Falha, StatusAnalise.AguardandoConfirmacao,
                   StatusAnalise.Confirmada, StatusAnalise.Corrigida], GatilhoAnalise.Cancelar, StatusAnalise.Cancelada)
        .Finais(StatusAnalise.Validada, StatusAnalise.Cancelada);

    public static readonly MaquinaDeEstados<StatusConferencia, GatilhoConferencia> Conferencia = new MaquinaDeEstados<StatusConferencia, GatilhoConferencia>()
        .Permitir(StatusConferencia.Aberta, GatilhoConferencia.Encerrar, StatusConferencia.EmValidacao)
        .Permitir(StatusConferencia.EmValidacao, GatilhoConferencia.Validar, StatusConferencia.Concluida)
        .Permitir(StatusConferencia.EmValidacao, GatilhoConferencia.Devolver, StatusConferencia.Aberta)
        .Permitir([StatusConferencia.Aberta, StatusConferencia.EmValidacao], GatilhoConferencia.Cancelar, StatusConferencia.Cancelada)
        .Finais(StatusConferencia.Concluida, StatusConferencia.Cancelada);
}
