namespace Mytril.Audit.Sdk;

/// <summary>
/// Persiste o AuditableEvent na event_outbox do serviço chamador.
/// Deve ser chamado dentro do mesmo escopo de transação da operação de negócio.
/// NÃO chama SaveChanges — responsabilidade do use case chamador.
/// Implementado pelo serviço que instala o SDK usando seu próprio DbContext.
/// </summary>
public interface IOutboxRepository
{
    Task AddAuditEventAsync(AuditableEvent envelope, CancellationToken ct = default);
}
