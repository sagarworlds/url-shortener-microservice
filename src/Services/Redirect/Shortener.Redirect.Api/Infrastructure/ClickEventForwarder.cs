using System.Text.Json;
using System.Threading.Channels;
using RabbitMQ.Client;
using Shortener.Contracts;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Drains <see cref="ChannelClickPublisher"/>'s channel and publishes each
/// event to the <c>link-clicks</c> exchange. The redirect hot path never
/// waits on this: it only ever touches the channel.
/// </summary>
public sealed class ClickEventForwarder(
    ChannelReader<LinkClicked> reader,
    IConnection connection,
    ILogger<ClickEventForwarder> logger) : BackgroundService
{
    private const string ExchangeName = "link-clicks";
    private const string RoutingKey = "link.clicked";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declared idempotently here too: Analytics (the consumer) may not
        // have started first.
        await channel.ExchangeDeclareAsync(
            ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await foreach (var clickEvent in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(clickEvent);
                var properties = new BasicProperties { Persistent = true };

                await channel.BasicPublishAsync(ExchangeName, RoutingKey, mandatory: false, properties, body, stoppingToken);
            }
            catch (Exception ex)
            {
                // Best-effort, matching IClickPublisher: losing an occasional
                // click is acceptable, so log and keep draining rather than
                // crash the forwarder loop.
                logger.LogWarning(ex, "Failed to publish click event for code {Code}", clickEvent.Code);
            }
        }
    }
}
