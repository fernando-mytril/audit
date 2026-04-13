using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mytril.Audit.Application.Diagnostics;

public static class AuditDiagnostics
{
    public const string ServiceName = "Mytril.Audit";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    // --- Counters: Ingestion ---
    public static readonly Counter<long> EventIngested =
        Meter.CreateCounter<long>("audit.event.ingested", description: "Audit events successfully ingested");

    public static readonly Counter<long> EventDuplicate =
        Meter.CreateCounter<long>("audit.event.duplicate", description: "Duplicate audit events detected");

    public static readonly Counter<long> EventInvalid =
        Meter.CreateCounter<long>("audit.event.invalid", description: "Invalid audit envelopes received");

    // --- Counters: Consumer ---
    public static readonly Counter<long> ConsumerReceived =
        Meter.CreateCounter<long>("audit.consumer.received", description: "Messages received from RabbitMQ");

    public static readonly Counter<long> ConsumerProcessed =
        Meter.CreateCounter<long>("audit.consumer.processed", description: "Messages successfully processed");

    public static readonly Counter<long> ConsumerFailed =
        Meter.CreateCounter<long>("audit.consumer.failed", description: "Messages that failed processing");

    public static readonly Counter<long> ConsumerRequeued =
        Meter.CreateCounter<long>("audit.consumer.requeued", description: "Messages requeued for retry");

    // --- Counters: Queries ---
    public static readonly Counter<long> QueryExecuted =
        Meter.CreateCounter<long>("audit.query.executed", description: "Audit queries executed");

    // --- Histograms ---
    public static readonly Histogram<double> UseCaseDuration =
        Meter.CreateHistogram<double>("audit.usecase.duration", "ms", "Use case/handler execution duration");
}
