using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mytril.MaterialId.Api.Seguranca;

namespace Mytril.MaterialId.Api.Endpoints;

/// <summary>
/// Emite tokens de teste no formato do Mytril Identity. Mapeado SOMENTE em Development,
/// enquanto o acesso ao Identity (DEP-01) não está disponível.
/// </summary>
public static class DevTokenEndpoint
{
    public sealed record DevTokenRequest(Guid EmpresaId, string Usuario, string Perfil);

    public static void MapDevToken(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.MapPost("/dev/token", (DevTokenRequest r, IConfiguration config, Microsoft.Extensions.Options.IOptions<IdentityOptions> identity) =>
        {
            var perms = r.Perfil.ToLowerInvariant() switch
            {
                "operador" => new[] { Permissoes.Capturar },
                "gestor" => [Permissoes.Capturar, Permissoes.Validar],
                "admin" => [Permissoes.GerenciarCatalogo],
                "ia" => [Permissoes.ResultadoIa],
                _ => throw new BadHttpRequestException("Perfil deve ser operador, gestor, admin ou ia.")
            };
            var claims = new List<Claim> { new("sub", r.Usuario), new("name", r.Usuario), new(identity.Value.ClaimEmpresa, r.EmpresaId.ToString()) };
            claims.AddRange(perms.Select(p => new Claim(Permissoes.Claim, p)));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var token = new JwtSecurityToken(config["Jwt:Issuer"], null, claims, expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }).AllowAnonymous().ExcludeFromDescription();
    }
}
