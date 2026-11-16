namespace Mytril.MaterialId.Api.Seguranca;

/// <summary>Permissões na claim "perm" do token do Mytril Identity (D27, E1-02).</summary>
public static class Permissoes
{
    public const string Claim = "perm";
    public const string Capturar = "materialid.conferencia.capturar";
    public const string Validar = "materialid.conferencia.validar";
    public const string GerenciarCatalogo = "materialid.catalogo.gerenciar";
    public const string ResultadoIa = "materialid.ia.resultado";
}

public static class Politicas
{
    /// <summary>Operador ou gestor: consultar e operar no pátio.</summary>
    public const string Operador = nameof(Operador);
    /// <summary>Gestor (inclui dono e diretoria, D39): validar, devolver, concluir.</summary>
    public const string Gestor = nameof(Gestor);
    public const string Leitura = nameof(Leitura);
    public const string Catalogo = nameof(Catalogo);
    /// <summary>Servidor de IA devolvendo resultado.</summary>
    public const string Ia = nameof(Ia);
}
