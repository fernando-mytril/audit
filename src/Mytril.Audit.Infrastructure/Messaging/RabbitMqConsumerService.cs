using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mytril.Audit.Application.UseCases.IngestAuditEvent;
using Mytril.Audit.Domain.Enums;
using Mytril.Audit.Domain.Exceptions;
using Mytril.Audit.Domain.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mytril.Audit.Infrastructure.Messaging;

public sealed class RabbitMqConsumerService : IHostedService, IAsyncDisposable
{
    private const string ExchangeName = "platform.audit.events";
    private const string QueueName = "platform.audit.ingest";
    private const string BindingKey = "audit.#";

    private const string DlxExchangeName = "platform.audit.dlx";
    private const string DlqQueueName = "platform.audit.dead-letter";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IConnectionFactory _connectionFactory;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        IConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerService> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare dead-letter exchange and queue
        await _channel.ExchangeDeclareAsync(
            exchange: DlxExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: DlqQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: DlqQueueName,
            exchange: DlxExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        // Declare main exchange and queue with dead-letter config
        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DlxExchangeName
        };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: BindingKey,
            cancellationToken: cancellationToken);

        // Set prefetch to process one message at a time
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer started — Exchange: {Exchange}, Queue: {Queue}",
            ExchangeName, QueueName);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);
            var envelope = JsonSerializer.Deserialize<AuditEventEnvelope>(body, JsonOptions);

            if (envelope is null)
                throw new InvalidAuditEnvelopeException("Failed to deserialize audit event envelope.");

            var command = new IngestAuditEventCommand(
                EventId: envelope.EventId,
                EventType: envelope.EventType,
                EventVersion: envelope.EventVersion ?? "1.0",
                Source: envelope.Source,
                Severity: envelope.Severity,
                TraceId: envelope.TraceId,
                Payload: envelope.Payload,
                OccurredAt: envelope.OccurredAt,
                UserId: envelope.UserId,
                ActorId: envelope.ActorId,
                TenantId: envelope.TenantId,
                TenantProductId: envelope.TenantProductId,
                IpAddress: envelope.IpAddress,
                UserAgent: envelope.UserAgent,
                CorrelationId: envelope.CorrelationId);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            if (await idempotencyStore.ExistsAsync(command.EventId))
            {
                _logger.LogDebug("Duplicate event skipped via idempotency store — EventId: {EventId}", command.EventId);
                await AckAsync(deliveryTag);
                return;
            }

            var useCase = scope.ServiceProvider.GetRequiredService<IngestAuditEventUseCase>();
            await useCase.ExecuteAsync(command);

            await idempotencyStore.MarkProcessedAsync(command.EventId);
            await AckAsync(deliveryTag);

            _logger.LogDebug("Audit event processed — EventId: {EventId}", command.EventId);
        }
        catch (InvalidAuditEnvelopeException ex)
        {
            _logger.LogError(ex, "Invalid audit envelope — sending to DLQ. DeliveryTag: {DeliveryTag}", deliveryTag);
            await NackAsync(deliveryTag, requeue: false);
        }
        catch (DuplicateAuditEventException ex)
        {
            _logger.LogDebug(ex, "Duplicate audit event — acknowledging. DeliveryTag: {DeliveryTag}", deliveryTag);
            await AckAsync(deliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transient error processing audit event — requeuing. DeliveryTag: {DeliveryTag}", deliveryTag);
            await NackAsync(deliveryTag, requeue: true);
        }
    }

    private async Task AckAsync(ulong deliveryTag)
    {
        if (_channel is not null)
            await _channel.BasicAckAsync(deliveryTag, multiple: false);
    }

    private async Task NackAsync(ulong deliveryTag, bool requeue)
    {
        if (_channel is not null)
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection = null;
        }

        _logger.LogInformation("RabbitMQ consumer stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Separate from SDK's AuditableEvent to avoid version coupling
    private sealed record AuditEventEnvelope
    {
        public Guid EventId { get; init; }
        public string EventType { get; init; } = default!;
        public string? EventVersion { get; init; }
        public string Source { get; init; } = default!;
        public AuditSeverity Severity { get; init; } = AuditSeverity.Info;
        public Guid? UserId { get; init; }
        public Guid? ActorId { get; init; }
        public Guid? TenantId { get; init; }
        public Guid? TenantProductId { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string TraceId { get; init; } = default!;
        public string? CorrelationId { get; init; }
        public string Payload { get; init; } = default!;
        public DateTime OccurredAt { get; init; }
    }
}
