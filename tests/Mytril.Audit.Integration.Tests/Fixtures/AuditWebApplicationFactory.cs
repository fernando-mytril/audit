using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mytril.Audit.Domain.Services;
using Mytril.Audit.Infrastructure.Persistence;
using StackExchange.Redis;
using Testcontainers.PostgreSql;

namespace Mytril.Audit.Integration.Tests.Fixtures;

public sealed class AuditWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("audit_test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the RabbitMQ hosted service so we don't need a RabbitMQ instance
            services.RemoveAll<IHostedService>();

            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<AuditDbContext>>();
            services.RemoveAll<AuditDbContext>();

            // Replace with Testcontainers PostgreSQL
            services.AddDbContext<AuditDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Remove Redis connection (IConnectionMultiplexer) to avoid needing Redis
            services.RemoveAll<IConnectionMultiplexer>();

            // Ensure in-memory idempotency store is used
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStoreStub>();

            // Ensure DB is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Simple in-memory idempotency store for integration tests.
    /// </summary>
    private sealed class InMemoryIdempotencyStoreStub : IIdempotencyStore
    {
        private readonly HashSet<Guid> _processed = [];

        public Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
            => Task.FromResult(_processed.Contains(eventId));

        public Task MarkProcessedAsync(Guid eventId, CancellationToken ct = default)
        {
            _processed.Add(eventId);
            return Task.CompletedTask;
        }
    }
}
