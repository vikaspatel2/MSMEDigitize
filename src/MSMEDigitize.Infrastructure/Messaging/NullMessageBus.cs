using MSMEDigitize.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MSMEDigitize.Infrastructure.Messaging;

/// <summary>No-op message bus used when RabbitMQ is not configured.</summary>
public class NullMessageBus : IMessageBus
{
    private readonly ILogger<NullMessageBus> _logger;
    public NullMessageBus(ILogger<NullMessageBus> logger) => _logger = logger;

    public Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default) where T : class
    {
        _logger.LogDebug("NullMessageBus: Dropped message {Type}", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
