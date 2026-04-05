namespace Mytril.Audit.Sdk;

public sealed class AuditProducerOptions
{
    public string Source { get; set; } = default!;
    public string RoutingKeyPrefix { get; set; } = "audit";
}
