namespace Shortener.Contracts;

/// <summary>
/// Raised by Links when a link is created, via its transactional outbox.
/// Consumed by Redirect to populate its Redis read model.
/// </summary>
/// <param name="EventId">Unique id for this occurrence, used by consumers to de-duplicate at-least-once delivery.</param>
/// <param name="Code">The short code.</param>
/// <param name="Url">The destination URL.</param>
/// <param name="ExpiresAt">When the link stops resolving, or <see langword="null"/> if it never expires.</param>
/// <param name="OccurredAt">When the link was created.</param>
/// <param name="OwnerId">The identity of the user who created this link, or <see langword="null"/> for pre-auth links.</param>
public sealed record LinkCreated(Guid EventId, string Code, string Url, DateTimeOffset? ExpiresAt, DateTimeOffset OccurredAt, string? OwnerId = null);
