using System.Text.Json;
using Shortener.Contracts;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>
/// An event awaiting dispatch to RabbitMQ, persisted transactionally
/// alongside the link change that raised it. Pure persistence bookkeeping,
/// not a domain concept — deliberately kept out of <c>Domain</c>, matching
/// Analytics' <c>ProcessedEvent</c> precedent.
/// </summary>
internal sealed class OutboxMessage
{
    private OutboxMessage(Guid id, string type, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    /// <summary>The id of the event this row carries (its own <c>EventId</c>).</summary>
    public Guid Id { get; }

    /// <summary>The event's CLR type name, used to pick the routing key when dispatching.</summary>
    public string Type { get; }

    /// <summary>The JSON-serialized event.</summary>
    public string Payload { get; }

    /// <summary>When the underlying event occurred (not when this row was written).</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>When this row was successfully published, or <see langword="null"/> if it's still pending.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>Wraps a <see cref="LinkCreated"/> event as an outbox row.</summary>
    /// <param name="createdEvent">The event to stage.</param>
    /// <returns>A new, undispatched <see cref="OutboxMessage"/>.</returns>
    public static OutboxMessage For(LinkCreated createdEvent) =>
        new(createdEvent.EventId, nameof(LinkCreated), JsonSerializer.Serialize(createdEvent), createdEvent.OccurredAt);

    /// <summary>Wraps a <see cref="LinkDeleted"/> event as an outbox row.</summary>
    /// <param name="deletedEvent">The event to stage.</param>
    /// <returns>A new, undispatched <see cref="OutboxMessage"/>.</returns>
    public static OutboxMessage For(LinkDeleted deletedEvent) =>
        new(deletedEvent.EventId, nameof(LinkDeleted), JsonSerializer.Serialize(deletedEvent), deletedEvent.OccurredAt);

    /// <summary>Marks this row as successfully published.</summary>
    /// <param name="dispatchedAt">When it was published.</param>
    public void MarkDispatched(DateTimeOffset dispatchedAt) => DispatchedAt = dispatchedAt;
}
