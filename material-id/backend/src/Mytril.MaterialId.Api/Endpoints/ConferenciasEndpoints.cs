using Mytril.MaterialId.Api.Seguranca;
using Mytril.MaterialId.Conferencias.Application;
using Mytril.MaterialId.Conferencias.Domain;

namespace Mytril.MaterialId.Api.Endpoints;

public static class ConferenciasEndpoints
{
    public static IEndpointRouteBuilder MapConferencias(this IEndpointRouteBuilder app)
    {
        var c = app.MapGroup("/api/conferencias").WithTags("Conferências");

        c.MapGet("/", (ConferenciaService s, StatusConferencia? status, CancellationToken ct) => s.ListarAsync(status, ct))
            .RequireAuthorization(Politicas.Leitura).WithSummary("Conferências da empresa");
        c.MapGet("/{id:guid}", (Guid id, ConferenciaService s, CancellationToken ct) => s.ObterAsync(id, ct))
            .RequireAuthorization(Politicas.Leitura).WithSummary("Detalhe com amarrados e linha do tempo");

        c.MapPost("/", async (AbrirConferenciaCommand cmd, ConferenciaService s, CancellationToken ct) =>
            {
                var r = await s.AbrirAsync(cmd, ct);
                return Results.Created($"/api/conferencias/{r.Resumo.Id}", r);
            })
            .RequireAuthorization(Politicas.Operador).WithSummary("Abre conferência de Recebimento ou Expedição");
        c.MapPost("/{id:guid}/encerrar", (Guid id, ConferenciaService s, CancellationToken ct) => s.EncerrarAsync(id, ct))
            .RequireAuthorization(Politicas.Operador);
        c.MapPost("/{id:guid}/validar", (Guid id, ConferenciaService s, CancellationToken ct) => s.ValidarAsync(id, ct))
            .RequireAuthorization(Politicas.Gestor);
        c.MapPost("/{id:guid}/devolver", (Guid id, MotivoCommand m, ConferenciaService s, CancellationToken ct) => s.DevolverAsync(id, m.Motivo, ct))
            .RequireAuthorization(Politicas.Gestor);
        c.MapPost("/{id:guid}/cancelar", (Guid id, MotivoCommand m, ConferenciaService s, CancellationToken ct) => s.CancelarAsync(id, m.Motivo, ct))
            .RequireAuthorization(Politicas.Operador);
        c.MapPost("/{id:guid}/analises", (Guid id, ConferenciaService s, CancellationToken ct) => s.AdicionarAnaliseAsync(id, ct))
            .RequireAuthorization(Politicas.Operador).WithSummary("Adiciona um amarrado ou pilha (status Capturando)");

        var a = app.MapGroup("/api/analises/{id:guid}").WithTags("Amarrados (Análises)");
        a.MapPost("/enviar-fotos", (Guid id, ConferenciaService s, CancellationToken ct) => s.EnviarFotosAsync(id, ct)).RequireAuthorization(Politicas.Operador);
        a.MapPost("/reprocessar", (Guid id, ConferenciaService s, CancellationToken ct) => s.ReprocessarAsync(id, ct)).RequireAuthorization(Politicas.Operador);
        a.MapPost("/refazer-fotos", (Guid id, ConferenciaService s, CancellationToken ct) => s.RefazerFotosAsync(id, ct)).RequireAuthorization(Politicas.Operador);
        a.MapPost("/confirmar", (Guid id, ConferenciaService s, CancellationToken ct) => s.ConfirmarAsync(id, ct)).RequireAuthorization(Politicas.Operador);
        a.MapPost("/corrigir", (Guid id, MedicaoCommand m, ConferenciaService s, CancellationToken ct) => s.CorrigirAsync(id, m, ct))
            .RequireAuthorization(Politicas.Operador).WithSummary("Nega o resultado informando o valor correto (obrigatório)");
        a.MapPost("/cancelar", (Guid id, MotivoCommand m, ConferenciaService s, CancellationToken ct) => s.CancelarAnaliseAsync(id, m.Motivo, ct)).RequireAuthorization(Politicas.Operador);
        a.MapPost("/validar", (Guid id, ConferenciaService s, CancellationToken ct) => s.ValidarAnaliseAsync(id, ct)).RequireAuthorization(Politicas.Gestor);
        a.MapPost("/devolver", (Guid id, MotivoCommand m, ConferenciaService s, CancellationToken ct) => s.DevolverAnaliseAsync(id, m.Motivo, ct)).RequireAuthorization(Politicas.Gestor);
        a.MapPost("/resultado-ia", (Guid id, ResultadoIaCommand r, ConferenciaService s, CancellationToken ct) => s.RegistrarResultadoIaAsync(id, r, ct))
            .RequireAuthorization(Politicas.Ia).WithSummary("Retorno do servidor de IA (contrato inicial do E4-01)");
        a.MapPost("/falha-ia", (Guid id, MotivoCommand m, ConferenciaService s, CancellationToken ct) => s.FalharAsync(id, m.Motivo, ct))
            .RequireAuthorization(Politicas.Ia);

        return app;
    }
}
