namespace Mytril.Audit.Infrastructure.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "mytril-audit";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "desenv";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public bool EnablePrometheusEndpoint { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool InstrumentEfCore { get; set; } = true;
    public bool InstrumentRedis { get; set; } = true;
}
