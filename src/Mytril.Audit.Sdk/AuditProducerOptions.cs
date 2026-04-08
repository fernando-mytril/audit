namespace Mytril.Audit.Sdk;

public sealed class AuditProducerOptions
{
    public const string SectionName = "AuditProducer";

    /// <summary>
    /// Identificador kebab-case do serviço. Deve ser único na plataforma.
    /// Exemplos: mytril-identity, mytril-billing, mytril-scheduler.
    /// </summary>
    public required string Source { get; init; }
}
