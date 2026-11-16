namespace Mytril.MaterialId.SharedKernel.Tenancy;

/// <summary>Entidade que pertence a uma empresa (E1-03). Toda consulta é filtrada pela empresa do usuário.</summary>
public interface IPertenceAEmpresa
{
    Guid EmpresaId { get; }
}

/// <summary>Empresa e usuário da requisição atual, vindos do token do Mytril Identity.</summary>
public interface IContextoUsuario
{
    Guid EmpresaId { get; }
    string Usuario { get; }
}
