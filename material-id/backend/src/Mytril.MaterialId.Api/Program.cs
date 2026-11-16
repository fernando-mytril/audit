using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mytril.MaterialId.Api.Endpoints;
using Mytril.MaterialId.Api.Infraestrutura;
using Mytril.MaterialId.Api.Seguranca;
using Mytril.MaterialId.Catalogo;
using Mytril.MaterialId.Catalogo.Infrastructure;
using Mytril.MaterialId.Conferencias;
using Mytril.MaterialId.Conferencias.Infrastructure;
using Mytril.MaterialId.SharedKernel;
using Mytril.MaterialId.SharedKernel.Tenancy;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Persistência (PostgreSQL, um schema por módulo) ----------
var conexao = config.GetConnectionString("MaterialId");
builder.Services.AddDbContext<CatalogoDbContext>(o => o.UseNpgsql(conexao,
    n => n.MigrationsHistoryTable("__ef_migrations", CatalogoDbContext.Schema)).UseSnakeCaseNamingConvention());
builder.Services.AddDbContext<ConferenciasDbContext>(o => o.UseNpgsql(conexao,
    n => n.MigrationsHistoryTable("__ef_migrations", ConferenciasDbContext.Schema)).UseSnakeCaseNamingConvention());

// ---------- Módulos ----------
builder.Services.AddModuloCatalogo();
builder.Services.AddModuloConferencias();
builder.Services.AddSingleton<IRelogio, RelogioDoSistema>();

// ---------- Segurança: JWT do Mytril Identity, igual ao Mytril.Audit (D27) ----------
builder.Services.Configure<IdentityOptions>(config.GetSection("Identity"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioHttp>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var chave = config["Jwt:Key"] ?? throw new InvalidOperationException("Configure Jwt:Key.");
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"] ?? "mytril-identity",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave))
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Leitura, p => p.RequireClaim(Permissoes.Claim, Permissoes.Capturar, Permissoes.Validar))
    .AddPolicy(Politicas.Operador, p => p.RequireClaim(Permissoes.Claim, Permissoes.Capturar))
    .AddPolicy(Politicas.Gestor, p => p.RequireClaim(Permissoes.Claim, Permissoes.Validar))
    .AddPolicy(Politicas.Catalogo, p => p.RequireClaim(Permissoes.Claim, Permissoes.GerenciarCatalogo))
    .AddPolicy(Politicas.Ia, p => p.RequireClaim(Permissoes.Claim, Permissoes.ResultadoIa));

// ---------- API ----------
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratamentoDeErros>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddNpgSql(conexao ?? string.Empty, name: "postgresql", tags: ["ready"]);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origens").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference(o => o.Title = "Mytril Material ID").AllowAnonymous();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new() { Predicate = h => h.Tags.Contains("ready") }).AllowAnonymous();

app.MapCatalogo();
app.MapConferencias();
app.MapDevToken();

if (config.GetValue<bool>("Banco:AplicarMigracoesNaInicializacao"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ConferenciasDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CatalogoSeeder>().CarregarAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
