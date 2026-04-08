using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mytril.Audit.Sdk;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra IAuditProducer com configuração via IConfiguration (seção "AuditProducer").
    /// O serviço chamador é responsável por registrar IOutboxRepository.
    /// </summary>
    public static IServiceCollection AddMytrilAuditProducer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuditProducerOptions>? configure = null)
    {
        var optionsBuilder = services
            .AddOptions<AuditProducerOptions>()
            .Bind(configuration.GetSection(AuditProducerOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Source),
                "AuditProducer:Source is required — must be kebab-case service identifier.");

        if (configure is not null)
            optionsBuilder.Configure(configure);

        optionsBuilder.ValidateOnStart();

        services.AddScoped<IAuditProducer, AuditProducer>();

        return services;
    }
}
