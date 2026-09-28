namespace Shortener.Links.Api.Domain;

/// <summary>A short link: a code mapped to a destination URL, with optional expiry.</summary>
public sealed class Link
{
    /// <summary>Creates a <see cref="Link"/> from already-validated parts.</summary>
    /// <param name="id">The link's unique identifier.</param>
    /// <param name="code">The short code.</param>
    /// <param name="destinationUrl">The URL the code redirects to.</param>
    /// <param name="createdAt">When the link was created.</param>
    /// <param name="expiresAt">When the link stops resolving, or <see langword="null"/> if it never expires.</param>
    /// <param name="ownerId">The identity of the user who created this link, or <see langword="null"/> for pre-auth links.</param>
    public Link(Guid id, ShortCode code, DestinationUrl destinationUrl, DateTimeOffset createdAt, DateTimeOffset? expiresAt, string? ownerId = null)
    {
        Id = id;
        Code = code;
        DestinationUrl = destinationUrl;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        OwnerId = ownerId;
    }

    /// <summary>The link's unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The short code that resolves to <see cref="DestinationUrl"/>.</summary>
    public ShortCode Code { get; }

    /// <summary>The URL this link redirects to.</summary>
    public DestinationUrl DestinationUrl { get; }

    /// <summary>When the link was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>When the link stops resolving, or <see langword="null"/> if it never expires.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>The identity of the user who created this link, or <see langword="null"/> for pre-auth links.</summary>
    public string? OwnerId { get; }

    /// <summary>Whether the link has expired as of <paramref name="asOf"/>.</summary>
    /// <param name="asOf">The instant to check expiry against.</param>
    /// <returns><see langword="true"/> if <see cref="ExpiresAt"/> is set and at or before <paramref name="asOf"/>.</returns>
    public bool IsExpired(DateTimeOffset asOf) => ExpiresAt is not null && ExpiresAt <= asOf;
}
