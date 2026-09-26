namespace Shortener.Contracts;

/// <summary>
/// Raised by Links when a link is deleted, via its transactional outbox.
/// Consumed by Redirect to remove the code from its Redis read model.
/// </summary>
/// <param name="EventId">Unique id for this occurrence, used by consumers to de-duplicate at-least-once delivery.</param>
/// <param name="Code">The short code that was deleted.</param>
/// <param name="OccurredAt">When the link was deleted.</param>
public sealed record LinkDeleted(Guid EventId, string Code, DateTimeOffset OccurredAt);
