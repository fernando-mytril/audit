namespace Mytril.MaterialId.SharedKernel.Exceptions;

/// <summary>Violação de regra de negócio. A API responde 422.</summary>
public class RegraDeNegocioException(string message) : Exception(message);

/// <summary>Recurso inexistente ou de outra empresa. A API responde 404.</summary>
public sealed class NaoEncontradoException(string message) : Exception(message);
