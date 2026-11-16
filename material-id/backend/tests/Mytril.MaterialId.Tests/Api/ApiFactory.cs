using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Mytril.MaterialId.Api.Seguranca;
using Mytril.MaterialId.Catalogo.Infrastructure;
using Mytril.MaterialId.Conferencias.Infrastructure;

namespace Mytril.MaterialId.Tests.Api;

/// <summary>API real em memória: SQLite no lugar do PostgreSQL e tokens no formato do Mytril Identity.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?>? configuracoes;

    public ApiFactory(IDictionary<string, string?>? configuracoes = null)
    {
        this.configuracoes = configuracoes;
        _conexao = new SqliteConnection(_cadeia);
        _conexao.Open();
    }

    public const string Chave = "chave-de-teste-com-mais-de-32-caracteres!!";
    // Banco em memória compartilhado: uma conexão fica aberta para mantê-lo vivo e cada contexto abre a sua,
    // porque a API e o processamento em segundo plano acessam o banco ao mesmo tempo.
    private readonly string _cadeia = $"DataSource=file:materialid-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _conexao;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", Chave);
        builder.UseSetting("Jwt:Issuer", "mytril-identity");
        builder.UseSetting("Banco:AplicarMigracoesNaInicializacao", "false");
        builder.UseSetting("ConnectionStrings:MaterialId", "Host=nao-usado");
        foreach (var (chave, valor) in configuracoes ?? new Dictionary<string, string?>()) builder.UseSetting(chave, valor);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogoDbContext>>();
            services.RemoveAll<DbContextOptions<ConferenciasDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CatalogoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ConferenciasDbContext>>();
            services.AddDbContext<CatalogoDbContext>(o => o.UseSqlite(_cadeia));
            services.AddDbContext<ConferenciasDbContext>(o => o.UseSqlite(_cadeia));
        });
    }

    public async Task InicializarAsync()
    {
        using var scope = Services.CreateScope();
        var catalogo = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        await catalogo.Database.EnsureCreatedAsync();
        var conf = new ConferenciasDbContext(new DbContextOptionsBuilder<ConferenciasDbContext>().UseSqlite(_conexao).Options, new ContextoFixo(Guid.Empty));
        foreach (var cmd in conf.Database.GenerateCreateScript().Split(";", StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)))
            await conf.Database.ExecuteSqlRawAsync(cmd);
        await new CatalogoSeeder(catalogo, NullLogger<CatalogoSeeder>.Instance).CarregarAsync(CancellationToken.None);
    }

    public HttpClient Cliente(Guid empresaId, string usuario, params string[] permissoes)
    {
        var claims = new List<Claim> { new("sub", usuario), new("name", usuario), new("tenant_id", empresaId.ToString()) };
        claims.AddRange(permissoes.Select(p => new Claim(Permissoes.Claim, p)));
        var token = new JwtSecurityToken("mytril-identity", null, claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave)), SecurityAlgorithms.HmacSha256));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conexao.Dispose();
    }
}
