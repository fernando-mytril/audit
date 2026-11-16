using System.Security.Claims;
using Microsoft.Extensions.Options;
using Mytril.MaterialId.SharedKernel.Tenancy;

namespace Mytril.MaterialId.Api.Seguranca;

public sealed class IdentityOptions
{
    /// <summary>Nome da claim da empresa no token. A confirmar no Mytril Identity (DEP-01).</summary>
    public string ClaimEmpresa { get; set; } = "tenant_id";
}

/// <summary>Empresa e usuário vindos do token. Sem empresa no token, a requisição é recusada (403).</summary>
public sealed class ContextoUsuarioHttp(IHttpContextAccessor http, IOptions<IdentityOptions> options, ContextoExecucao execucao) : IContextoUsuario
{
    private ClaimsPrincipal Principal => http.HttpContext?.User ?? throw new UnauthorizedAccessException("Requisição sem usuário.");

    public Guid EmpresaId => execucao.EmpresaId ?? EmpresaDoToken;

    public string Usuario => execucao.Usuario ?? UsuarioDoToken;

    private Guid EmpresaDoToken =>
        Guid.TryParse(Principal.FindFirstValue(options.Value.ClaimEmpresa), out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("O token não informa a empresa do usuário.");

    private string UsuarioDoToken =>
        Principal.FindFirstValue("name") ?? Principal.FindFirstValue(ClaimTypes.Name) ??
        Principal.FindFirstValue("email") ?? Principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
        Principal.FindFirstValue("sub") ?? "desconhecido";
}
