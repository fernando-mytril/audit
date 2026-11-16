using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mytril.MaterialId.Api.Seguranca;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Catalogo.Domain;
using Mytril.MaterialId.Conferencias.Application;
using Mytril.MaterialId.Conferencias.Domain;
using Mytril.MaterialId.Conferencias.Infrastructure;

namespace Mytril.MaterialId.Api.Infraestrutura;

public sealed class IaSimuladaOptions
{
    public bool Habilitada { get; set; }
    /// <summary>Tempo simulado de processamento de cada amarrado.</summary>
    public int SegundosDeProcessamento { get; set; } = 3;
    /// <summary>Fração de análises que falham, para testar o fluxo de Falha.</summary>
    public double TaxaDeFalha { get; set; } = 0.1;
}

/// <summary>
/// SOMENTE PARA TESTES: substitui o servidor de IA (E4-01) enquanto ele não existe.
/// Os amarrados em "Processando" recebem um produto do catálogo, uma quantidade próxima à do
/// amarrado padrão e uma confiança aleatória. O resultado é determinístico por amarrado e
/// gravado com a versão de modelo "simulador-dev".
/// </summary>
public sealed class IaSimulada(IServiceScopeFactory scopes, IOptions<IaSimuladaOptions> options, ILogger<IaSimulada> logger) : BackgroundService
{
    public const string VersaoModelo = "simulador-dev";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogWarning("IA SIMULADA ATIVA: resultados de identificação são fictícios. Não use em produção.");
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessarPendentesAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Falha na IA simulada"); }
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    private async Task ProcessarPendentesAsync(CancellationToken ct)
    {
        List<(Guid Id, Guid EmpresaId)> prontas;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConferenciasDbContext>();
            var limite = DateTimeOffset.UtcNow.AddSeconds(-options.Value.SegundosDeProcessamento);
            var processando = await db.Analises.IgnoreQueryFilters().AsNoTracking()
                .Where(a => a.Status == StatusAnalise.Processando).ToListAsync(ct);
            prontas = processando.Where(a => a.Transicoes.Max(t => t.Em) <= limite).Select(a => (a.Id, a.EmpresaId)).ToList();
        }

        foreach (var (id, empresaId) in prontas)
        {
            using var scope = scopes.CreateScope();
            scope.ServiceProvider.GetRequiredService<ContextoExecucao>().Definir(empresaId, ConferenciaService.UsuarioSistemaIa);
            var servico = scope.ServiceProvider.GetRequiredService<ConferenciaService>();
            var catalogo = scope.ServiceProvider.GetRequiredService<ICatalogoConsulta>();
            var aleatorio = new Random(id.GetHashCode());

            if (aleatorio.NextDouble() < options.Value.TaxaDeFalha)
            {
                await servico.FalharAsync(id, "Foto escura ou fora de foco (falha simulada). Refaça as fotos ou reprocesse.", ct);
                continue;
            }

            var itens = await catalogo.ListarItensAsync(new FiltroCatalogo(RequerRevisao: false), ct);
            var item = itens[aleatorio.Next(itens.Count)];
            var padrao = item.PecasPorAmarrado ?? 20;
            var quantidade = Math.Max(1, padrao + aleatorio.Next(-3, 4));
            var (comprimento, largura) = item.TipoPeso == TipoPeso.KgPorMetroQuadrado ? (3000m, 1200m) : ((decimal?)null, (decimal?)null);
            if (item.TipoPeso == TipoPeso.KgPorMetroQuadrado) quantidade = aleatorio.Next(5, 30);
            var confianca = Math.Round((decimal)(0.5 + aleatorio.NextDouble() * 0.45), 2);

            await servico.RegistrarResultadoIaAsync(id,
                new ResultadoIaCommand(new MedicaoCommand(item.Id, quantidade, comprimento, largura), confianca, VersaoModelo), ct);
            logger.LogInformation("IA simulada: amarrado {Id} → {Produto}, {Quantidade} peças.", id, item.Descricao, quantidade);
        }
    }
}
