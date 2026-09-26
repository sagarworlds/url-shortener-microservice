namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>
/// Marks a <see cref="Shortener.Contracts.LinkClicked"/> event's <c>EventId</c>
/// as already applied, so at-least-once redelivery of the same event is a
/// no-op. Pure persistence bookkeeping, not a domain concept — deliberately
/// kept out of <c>Domain</c>.
/// </summary>
internal sealed class ProcessedEvent(Guid eventId, DateTimeOffset processedAt)
{
    public Guid EventId { get; } = eventId;

    public DateTimeOffset ProcessedAt { get; } = processedAt;
}
