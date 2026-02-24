using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MSMEDigitize.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _publishChannel;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMQMessageBus(IConnection connection, ILogger<RabbitMQMessageBus> logger)
    {
        _connection = connection;
        _publishChannel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var exchange = topic ?? "default";
            await _publishChannel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _publishChannel.BasicPublishAsync(exchange, topic ?? "", false, props, body);
            _logger.LogDebug("Published message {Type} to {Exchange}", typeof(T).Name, exchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {Type}", typeof(T).Name);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class
    {
        var channel = await _connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(topic, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (message != null)
                    await handler(message);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", topic);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(topic, false, consumer);
    }

    public void Dispose()
    {
        _publishChannel?.Dispose();
        _connection?.Dispose();
    }
}

// Message events
public record InvoiceCreatedEvent(Guid TenantId, Guid InvoiceId, string InvoiceNumber, decimal Amount, string CustomerEmail, string CustomerName);
public record PaymentReceivedEvent(Guid TenantId, Guid PaymentId, decimal Amount, string CustomerEmail);
public record LowStockAlertEvent(Guid TenantId, Guid ProductId, string ProductName, decimal CurrentStock, decimal MinStock);
public record SubscriptionExpiryEvent(Guid TenantId, DateTime ExpiryDate, string PlanName);
public record ComplianceDueEvent(Guid TenantId, string ComplianceName, DateTime DueDate);
public record NewTenantRegisteredEvent(Guid TenantId, string BusinessName, string Email);