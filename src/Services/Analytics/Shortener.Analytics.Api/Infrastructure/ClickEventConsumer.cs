using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shortener.Analytics.Api.Application;
using Shortener.Contracts;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>
/// Consumes <see cref="LinkClicked"/> events published to the
/// <c>link-clicks</c> exchange. Declares its own queue, dead-letter
/// exchange/queue and binding, since it owns them; the exchange itself is
/// declared idempotently here too, since Redirect (the producer) may not
/// have started first.
/// </summary>
public sealed class ClickEventConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ClickEventConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "link-clicks";
    private const string RoutingKey = "link.clicked";
    private const string QueueName = "link-clicks.analytics";
    private const string DeadLetterExchangeName = "link-clicks.dlx";
    private const string DeadLetterQueueName = "link-clicks.analytics.dead-letter";
    private const ushort PrefetchCount = 10;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            DeadLetterExchangeName,
            ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            DeadLetterQueueName,
            DeadLetterExchangeName,
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchangeName },
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Keep the channel and consumer alive until the host shuts down.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        // This is the consumer's equivalent of the HTTP GlobalExceptionHandler:
        // the one sanctioned catch-all, whose job is to turn any failure
        // (bad payload, a transient DB error, anything) into a defined
        // outcome — dead-lettering the message — rather than losing the
        // delivery's ack/nack decision or crashing the background service.
        try
        {
            var clickEvent = JsonSerializer.Deserialize<LinkClicked>(delivery.Body.Span)
                ?? throw new InvalidOperationException("Message body deserialized to null.");

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<RecordClickHandler>();
            await handler.HandleAsync(clickEvent, stoppingToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to process click event; dead-lettering delivery {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
    }
}
