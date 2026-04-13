using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;

namespace Mytril.Audit.Infrastructure.Observability;

public sealed class RedisInstrumentationHostedService(
    IServiceProvider serviceProvider,
    ILogger<RedisInstrumentationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
        if (multiplexer is null)
        {
            logger.LogInformation("Redis not configured — skipping Redis instrumentation.");
            return Task.CompletedTask;
        }

        var instrumentation = serviceProvider.GetService<StackExchangeRedisInstrumentation>();
        if (instrumentation is not null)
        {
            instrumentation.AddConnection(multiplexer);
            logger.LogInformation("Redis instrumentation connected.");
        }
        else
        {
            logger.LogWarning(
                "Redis IConnectionMultiplexer found but StackExchangeRedisInstrumentation is not registered — Redis traces will not be collected.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
