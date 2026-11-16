using Microsoft.Extensions.DependencyInjection;
using Mytril.MaterialId.Catalogo.Application;
using Mytril.MaterialId.Catalogo.Infrastructure;

namespace Mytril.MaterialId.Catalogo;

public static class DependencyInjection
{
    public static IServiceCollection AddModuloCatalogo(this IServiceCollection services)
    {
        services.AddScoped<ICatalogoConsulta, CatalogoConsulta>();
        services.AddScoped<CatalogoSeeder>();
        return services;
    }
}
