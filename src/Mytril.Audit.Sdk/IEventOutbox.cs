namespace Mytril.Audit.Sdk;

public interface IEventOutbox
{
    Task PublishAsync(string destination, string routingKey, string body, CancellationToken ct = default);
}
