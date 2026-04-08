namespace Mytril.Audit.Sdk;

public sealed record AuditableEvent
{
    // ── Identity ──────────────────────────────────────────────────────────
    /// <summary>UUID v4 único por evento — gerado automaticamente pelo SDK.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Tipo do evento. Convenção: {DOMINIO}_{ENTIDADE}_{ACAO}.
    /// Ex: AUTH_LOGIN_SUCCESS, BILLING_PAYMENT_PROCESSED.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>Versão do schema do evento. Default "1.0".</summary>
    public string EventVersion { get; init; } = "1.0";

    // ── Identidade do produtor ────────────────────────────────────────────
    /// <summary>
    /// Identificador kebab-case do serviço. Preenchido pelo SDK via AuditProducerOptions.Source.
    /// </summary>
    public required string Source { get; init; }

    // ── Severidade ────────────────────────────────────────────────────────
    public AuditSeverity Severity { get; init; } = AuditSeverity.Info;

    // ── Contexto do ator (todos opcionais) ───────────────────────────────
    public Guid?   UserId          { get; init; }
    public Guid?   ActorId         { get; init; }
    public Guid?   TenantId        { get; init; }
    public Guid?   TenantProductId { get; init; }
    public string? IpAddress       { get; init; }
    public string? UserAgent       { get; init; }

    // ── Rastreabilidade ──────────────────────────────────────────────────
    /// <summary>TraceId — propagado do contexto de rastreamento distribuído.</summary>
    public required string TraceId { get; init; }

    /// <summary>Correlation ID do cliente original.</summary>
    public string? CorrelationId { get; init; }

    // ── Payload ──────────────────────────────────────────────────────────
    /// <summary>
    /// Dados específicos do evento em JSON.
    /// Nunca incluir: passwords, tokens, hashes, segredos, dados bancários.
    /// </summary>
    public required string Payload { get; init; }

    // ── Timestamp ────────────────────────────────────────────────────────
    /// <summary>Momento em que o evento ocorreu no domínio produtor (UTC). Preenchido pelo SDK.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
