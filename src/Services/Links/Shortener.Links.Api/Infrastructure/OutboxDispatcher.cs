using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shortener.Contracts;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>
/// Polls for undispatched <see cref="OutboxMessage"/> rows and publishes
/// them to the <c>link-events</c> exchange, marking each dispatched
/// immediately after a successful publish. A crash mid-batch only loses the
/// mark for rows not yet published, which the next poll simply retries —
/// losing a link event isn't acceptable, so a failed publish leaves a row
/// unmarked rather than skipping it.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IConnection connection,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const string ExchangeName = "link-events";
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

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

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            await DispatchPendingAsync(channel, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchPendingAsync(IChannel channel, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LinksDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(message => message.DispatchedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in pending)
        {
            try
            {
                var routingKey = RoutingKeyFor(message.Type);
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var properties = new BasicProperties { Persistent = true };

                await channel.BasicPublishAsync(ExchangeName, routingKey, mandatory: false, properties, body, stoppingToken);

                message.MarkDispatched(timeProvider.GetUtcNow());
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispatch outbox message {MessageId}; will retry next poll", message.Id);
            }
        }
    }

    private static string RoutingKeyFor(string type) => type switch
    {
        nameof(LinkCreated) => "link.created",
        nameof(LinkDeleted) => "link.deleted",
        _ => throw new InvalidOperationException($"Unknown outbox message type: {type}"),
    };
}
