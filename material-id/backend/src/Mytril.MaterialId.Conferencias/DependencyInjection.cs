using Microsoft.Extensions.DependencyInjection;
using Mytril.MaterialId.Conferencias.Application;

namespace Mytril.MaterialId.Conferencias;

public static class DependencyInjection
{
    public static IServiceCollection AddModuloConferencias(this IServiceCollection services)
    {
        services.AddScoped<ConferenciaService>();
        return services;
    }
}
