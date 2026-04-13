using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mytril.Audit.Application.Diagnostics;
using Mytril.Audit.Infrastructure.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mytril.Audit.Infrastructure.DependencyInjection;

public static class ObservabilityExtensions
{

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        services.Configure<ObservabilityOptions>(
            configuration.GetSection(ObservabilityOptions.SectionName));

        var otlpEndpoint = new Uri(options.OtlpEndpoint);

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", options.Environment)
                }));

        if (options.EnableTracing)
        {
            otel.WithTracing(tracing =>
            {
                tracing
                    .AddSource(AuditDiagnostics.ServiceName)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/metrics") &&
                            !httpContext.Request.Path.StartsWithSegments("/openapi") &&
                            !httpContext.Request.Path.StartsWithSegments("/scalar");
                    })
                    .AddHttpClientInstrumentation();

                if (options.InstrumentEfCore)
                    tracing.AddEntityFrameworkCoreInstrumentation();

                if (options.InstrumentRedis)
                    tracing.AddRedisInstrumentation();

                tracing.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = otlpEndpoint;
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        }

        if (options.EnableMetrics)
        {
            otel.WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(AuditDiagnostics.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.EnablePrometheusEndpoint)
                    metrics.AddPrometheusExporter();

                metrics.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = otlpEndpoint;
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                });
            });
        }

        if (options.EnableTracing && options.InstrumentRedis)
            services.AddHostedService<RedisInstrumentationHostedService>();

        return services;
    }
}
