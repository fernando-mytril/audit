using Microsoft.Extensions.DependencyInjection;

namespace Mytril.Audit.Sdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMytrilAuditProducer(
        this IServiceCollection services,
        Action<AuditProducerOptions> configure)
    {
        services.Configure(configure);
        services.AddScoped<IAuditProducer, AuditProducer>();

        return services;
    }
}
