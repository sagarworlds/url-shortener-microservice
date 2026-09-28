using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shortener.Analytics.Api.Application;
using Shortener.Contracts;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>
/// Consumes <see cref="LinkCreated"/>/<see cref="LinkDeleted"/> events
/// published to the <c>link-events</c> exchange and maintains the
/// <c>link_owners</c> projection. This lets Analytics enforce ownership
/// checks without crossing the database-per-service boundary.
/// </summary>
internal sealed class LinkOwnerEventConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<LinkOwnerEventConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "link-events";
    private const string CreatedRoutingKey = "link.created";
    private const string DeletedRoutingKey = "link.deleted";
    private const string QueueName = "link-events.analytics";
    private const string DeadLetterExchangeName = "link-events.analytics.dlx";
    private const string DeadLetterQueueName = "link-events.analytics.dead-letter";
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

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            switch (delivery.RoutingKey)
            {
                case CreatedRoutingKey:
                {
                    var createdEvent = JsonSerializer.Deserialize<LinkCreated>(delivery.Body.Span)
                        ?? throw new InvalidOperationException("Message body deserialized to null.");

                    if (createdEvent.OwnerId is not null)
                    {
                        using var scope = scopeFactory.CreateScope();
                        var store = scope.ServiceProvider.GetRequiredService<ILinkOwnerStore>();
                        await store.SetOwnerAsync(createdEvent.Code, createdEvent.OwnerId, stoppingToken);
                    }

                    break;
                }

                case DeletedRoutingKey:
                {
                    var deletedEvent = JsonSerializer.Deserialize<LinkDeleted>(delivery.Body.Span)
                        ?? throw new InvalidOperationException("Message body deserialized to null.");

                    using var scope = scopeFactory.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<ILinkOwnerStore>();
                    await store.RemoveOwnerAsync(deletedEvent.Code, stoppingToken);
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unexpected routing key: {delivery.RoutingKey}");
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to process link owner event; dead-lettering delivery {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
    }
}
