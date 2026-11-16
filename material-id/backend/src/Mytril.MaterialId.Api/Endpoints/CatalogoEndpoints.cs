using Mytril.MaterialId.Api.Seguranca;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Catalogo.Domain;

namespace Mytril.MaterialId.Api.Endpoints;

public static class CatalogoEndpoints
{
    public sealed record CalcularPesoRequest(Guid ItemId, int Quantidade, decimal? ComprimentoMm, decimal? LarguraMm);

    public static IEndpointRouteBuilder MapCatalogo(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/catalogo").WithTags("Catálogo").RequireAuthorization(Politicas.Leitura);

        g.MapGet("/fabricantes", (ICatalogoConsulta c, CancellationToken ct) => c.ListarFabricantesAsync(ct))
            .WithSummary("Fabricantes do catálogo da plataforma");

        g.MapGet("/itens", (ICatalogoConsulta c, Guid? fabricanteId, Familia? familia, string? busca, bool? requerRevisao, CancellationToken ct) =>
                c.ListarItensAsync(new FiltroCatalogo(fabricanteId, familia, busca, requerRevisao), ct))
            .WithSummary("Itens do catálogo, com filtros por fabricante, família e busca por medida");

        g.MapGet("/itens/{id:guid}", async (Guid id, ICatalogoConsulta c, CancellationToken ct) =>
                await c.ObterItemAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Um item do catálogo");

        g.MapPost("/peso", (CalcularPesoRequest r, ICatalogoConsulta c, CancellationToken ct) =>
                c.CalcularPesoAsync(r.ItemId, r.Quantidade, r.ComprimentoMm, r.LarguraMm, ct))
            .WithSummary("Calcula o peso pela tabela do fabricante (E4-03)");

        return app;
    }
}
