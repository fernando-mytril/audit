namespace Mytril.MaterialId.Conferencias.Domain;

public enum Operacao
{
    Recebimento,
    Expedicao
}

/// <summary>Status da Conferência (D19, E3-01).</summary>
public enum StatusConferencia
{
    Aberta,
    EmValidacao,
    Concluida,
    Cancelada
}

public enum GatilhoConferencia
{
    Encerrar,
    Validar,
    Devolver,
    Cancelar
}

/// <summary>Status da Análise de um amarrado ou pilha (D17, E4-04).</summary>
public enum StatusAnalise
{
    Capturando,
    Processando,
    Falha,
    AguardandoConfirmacao,
    Confirmada,
    Corrigida,
    Validada,
    Cancelada
}

public enum GatilhoAnalise
{
    EnviarFotos,
    Concluir,
    Falhar,
    Reprocessar,
    RefazerFotos,
    Confirmar,
    Corrigir,
    Validar,
    Devolver,
    Cancelar
}
