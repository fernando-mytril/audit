using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Mytril.Audit.Api.Constants;
using Mytril.Audit.Api.Middlewares;
using Mytril.Audit.Infrastructure.DependencyInjection;
using Mytril.Audit.Infrastructure.Observability;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var obsOptions = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build()
    .GetSection(ObservabilityOptions.SectionName)
    .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

var logConfig = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .Enrich.FromLogContext();

if (obsOptions.EnableLogging)
{
    logConfig.WriteTo.OpenTelemetry(opts =>
    {
        opts.Endpoint = obsOptions.OtlpEndpoint;
        opts.Protocol = OtlpProtocol.Grpc;
        opts.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = obsOptions.ServiceName,
            ["service.version"] = obsOptions.ServiceVersion,
            ["deployment.environment"] = obsOptions.Environment
        };
    });
}

Log.Logger = logConfig.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddControllers();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? "super-secret-key-for-dev-only-32chars!!";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "mytril-identity",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

// Authorization
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAuditReadPermission", policy => policy.RequireClaim("perm", "admin.audit.read"));

// OpenAPI + Scalar
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = ApiConstants.Title,
            Version = "v1",
            Description = "Domain-Agnostic Audit Service — Universal Event Audit Log"
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
        };
        document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference(options => options.Title = ApiConstants.Title).AllowAnonymous();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health");

if (obsOptions.EnablePrometheusEndpoint && obsOptions.EnableMetrics)
    app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

public partial class Program { }
