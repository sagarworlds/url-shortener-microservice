using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shortener.Contracts;
using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Consumes <see cref="LinkCreated"/>/<see cref="LinkDeleted"/> events
/// published to the <c>link-events</c> exchange and applies them to the
/// Redis read model. Declares its own queue, dead-letter exchange/queue and
/// bindings, since it owns them; the exchange itself is declared
/// idempotently here too, since Links (the producer) may not have started
/// first.
/// </summary>
internal sealed class LinkEventConsumer(
    IConnection connection,
    IReadModelStore readModelStore,
    TimeProvider timeProvider,
    ILogger<LinkEventConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "link-events";
    private const string CreatedRoutingKey = "link.created";
    private const string DeletedRoutingKey = "link.deleted";
    private const string QueueName = "link-events.redirect";
    private const string DeadLetterExchangeName = "link-events.dlx";
    private const string DeadLetterQueueName = "link-events.redirect.dead-letter";
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
        await channel.QueueBindAsync(QueueName, ExchangeName, CreatedRoutingKey, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, DeletedRoutingKey, cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Keep the channel and consumer alive until the host shuts down.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        // The consumer's equivalent of the HTTP GlobalExceptionHandler: the
        // one sanctioned catch-all, turning any failure into a defined
        // outcome (dead-lettering) instead of losing the ack/nack decision.
        try
        {
            switch (delivery.RoutingKey)
            {
                case CreatedRoutingKey:
                {
                    // Deserialize before the first `await`: ReadOnlySpan<byte>
                    // is a ref struct and can't cross an async boundary.
                    var createdEvent = JsonSerializer.Deserialize<LinkCreated>(delivery.Body.Span)
                        ?? throw new InvalidOperationException("Message body deserialized to null.");
                    await HandleLinkCreatedAsync(createdEvent, stoppingToken);
                    break;
                }

                case DeletedRoutingKey:
                {
                    var deletedEvent = JsonSerializer.Deserialize<LinkDeleted>(delivery.Body.Span)
                        ?? throw new InvalidOperationException("Message body deserialized to null.");
                    await readModelStore.DeleteAsync(deletedEvent.Code, stoppingToken);
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unexpected routing key: {delivery.RoutingKey}");
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to process link event; dead-lettering delivery {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
    }

    private async Task HandleLinkCreatedAsync(LinkCreated createdEvent, CancellationToken stoppingToken)
    {
        if (createdEvent.ExpiresAt is { } expiresAt)
        {
            var ttl = expiresAt - timeProvider.GetUtcNow();
            if (ttl <= TimeSpan.Zero)
            {
                // Already expired (e.g. a backfilled old link) — never valid,
                // so it has no business entering the read model at all.
                return;
            }

            await readModelStore.SetAsync(createdEvent.Code, createdEvent.Url, ttl, stoppingToken);
            return;
        }

        await readModelStore.SetAsync(createdEvent.Code, createdEvent.Url, null, stoppingToken);
    }
}
