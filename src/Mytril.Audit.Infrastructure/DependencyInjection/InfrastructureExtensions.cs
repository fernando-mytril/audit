using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mytril.Audit.Application.Queries.GetAuditByActor;
using Mytril.Audit.Application.Queries.GetAuditByFilter;
using Mytril.Audit.Application.Queries.GetAuditBySource;
using Mytril.Audit.Application.Queries.GetAuditByTenant;
using Mytril.Audit.Application.Queries.GetAuditByTraceId;
using Mytril.Audit.Application.Queries.GetAuditByUser;
using Mytril.Audit.Application.UseCases.IngestAuditEvent;
using Mytril.Audit.Domain.Repositories;
using Mytril.Audit.Domain.Services;
using Mytril.Audit.Infrastructure.Idempotency;
using Mytril.Audit.Infrastructure.Messaging;
using Mytril.Audit.Infrastructure.Persistence;
using Mytril.Audit.Infrastructure.Persistence.Repositories;
using Mytril.Audit.Infrastructure.Security;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Mytril.Audit.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- PostgreSQL + EF Core ---
        var postgresConnection = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Connection string 'PostgreSql' is required.");

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(postgresConnection));

        // --- Repositories ---
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAuditQueryRepository, AuditQueryRepository>();

        // --- Payload sanitizer ---
        services.AddSingleton<IPayloadSanitizer, PayloadSanitizer>();

        // --- Redis (idempotency) ---
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));

            services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        }
        else
        {
            services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        }

        // --- RabbitMQ ---
        var rabbitConnection = configuration.GetConnectionString("RabbitMq");

        if (!string.IsNullOrWhiteSpace(rabbitConnection))
        {
            services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
            {
                Uri = new Uri(rabbitConnection)
            });

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddHostedService<RabbitMqConsumerService>();
        }

        // --- Health Checks ---
        var hc = services.AddHealthChecks()
            .AddDbContextCheck<AuditDbContext>("postgres");

        if (!string.IsNullOrWhiteSpace(redisConnection))
            hc.AddRedis(redisConnection, name: "redis");

        if (!string.IsNullOrWhiteSpace(rabbitConnection))
            hc.AddRabbitMQ(name: "rabbitmq");

        // --- Application: Use Cases ---
        services.AddScoped<IngestAuditEventUseCase>();

        // --- Application: Query Handlers ---
        services.AddScoped<GetAuditByActorHandler>();
        services.AddScoped<GetAuditByFilterHandler>();
        services.AddScoped<GetAuditBySourceHandler>();
        services.AddScoped<GetAuditByTenantHandler>();
        services.AddScoped<GetAuditByTraceIdHandler>();
        services.AddScoped<GetAuditByUserHandler>();

        return services;
    }
}
