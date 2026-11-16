using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.SharedKernel.StateMachines;

namespace Mytril.MaterialId.Conferencias.Application;

public sealed record AbrirConferenciaCommand(Operacao Operacao, string? Contraparte);

public sealed record MedicaoCommand(Guid ItemCatalogoId, int Quantidade, decimal? ComprimentoMm, decimal? LarguraMm);

/// <summary>Resultado devolvido pelo servidor de IA (contrato inicial do E4-01).</summary>
public sealed record ResultadoIaCommand(MedicaoCommand Medicao, decimal Confianca, string VersaoModelo);

public sealed record MotivoCommand(string Motivo);

public sealed record MedicaoDto(Guid ItemCatalogoId, string? Descricao, int Quantidade, decimal? ComprimentoMm, decimal? LarguraMm, decimal PesoKg);

public sealed record TransicaoDto(string De, string Para, string Gatilho, string Usuario, DateTimeOffset Em, string? Motivo)
{
    public static TransicaoDto From(Transicao t) => new(t.De, t.Para, t.Gatilho, t.Usuario, t.Em, t.Motivo);
}

public sealed record AnaliseDto(
    Guid Id, int Sequencia, StatusAnalise Status, MedicaoDto? Resultado, MedicaoDto? Correcao, MedicaoDto? MedicaoAtual,
    decimal? Confianca, string? VersaoModelo, string? MotivoFalha, IReadOnlyList<GatilhoAnalise> GatilhosValidos,
    IReadOnlyList<TransicaoDto> Transicoes);

public sealed record ConferenciaResumoDto(
    Guid Id, string Codigo, Operacao Operacao, string? Contraparte, StatusConferencia Status, DateTimeOffset CriadaEm,
    string CriadaPor, int QuantidadeAmarrados, int QuantidadePecas, decimal PesoMedidoKg,
    IReadOnlyList<GatilhoConferencia> GatilhosValidos);

public sealed record ConferenciaDetalheDto(ConferenciaResumoDto Resumo, IReadOnlyList<AnaliseDto> Analises, IReadOnlyList<TransicaoDto> Transicoes);
