namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>The cached outcome for a short code: found, absent, or expired.</summary>
internal enum CachedLinkStatus
{
    /// <summary>A live destination was cached.</summary>
    Found,

    /// <summary>No link has this code, as of the last check.</summary>
    NotFound,

    /// <summary>The link exists but had already expired at the last check.</summary>
    Expired,
}

/// <summary>Redis cache payload for a short code's resolution.</summary>
/// <param name="Status">Which outcome was cached.</param>
/// <param name="DestinationUrl">The destination URL when <paramref name="Status"/> is <see cref="CachedLinkStatus.Found"/>; otherwise <see langword="null"/>.</param>
internal sealed record CachedLink(CachedLinkStatus Status, string? DestinationUrl)
{
    /// <summary>Creates a cache entry for a live destination.</summary>
    /// <param name="url">The destination URL.</param>
    /// <returns>A <see cref="CachedLinkStatus.Found"/> entry.</returns>
    public static CachedLink Found(string url) => new(CachedLinkStatus.Found, url);

    /// <summary>The cache entry for a code with no link.</summary>
    public static CachedLink NotFound { get; } = new(CachedLinkStatus.NotFound, null);

    /// <summary>The cache entry for a code whose link has expired.</summary>
    public static CachedLink Expired { get; } = new(CachedLinkStatus.Expired, null);
}
