using Polly.CircuitBreaker;
using Polly.Timeout;
using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Resolves short codes by checking the cache first and falling back to
/// Links over HTTP on a miss. Used until M3 replaces it with a
/// <c>ReadModelLinkResolver</c> fed by <c>LinkCreated</c>/<c>LinkDeleted</c>
/// events instead of a direct HTTP dependency on Links.
/// </summary>
internal sealed class CacheAsideLinkResolver(
    ILinkCache cache,
    LinksClient linksClient,
    TimeProvider timeProvider,
    ILogger<CacheAsideLinkResolver> logger) : ILinkResolver
{
    /// <summary>The cap on how long a found link is cached, regardless of how far away its expiry is.</summary>
    internal static readonly TimeSpan MaxFoundTtl = TimeSpan.FromHours(24);

    /// <summary>How long a not-found or expired outcome is cached, to absorb repeat lookups without hammering Links.</summary>
    internal static readonly TimeSpan NegativeTtl = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    public async Task<LinkResolution> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(code, cancellationToken);
        if (cached is not null)
        {
            return ToResolution(cached);
        }

        LinkDetails? link;
        try
        {
            link = await linksClient.GetLinkAsync(code, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
        {
            // An outage must never become a 404: report unavailable and cache
            // nothing, so the next request tries Links again immediately.
            logger.LogWarning(ex, "Links unreachable resolving code {Code}", code);
            return LinkResolution.Unavailable();
        }

        if (link is null)
        {
            await cache.SetAsync(code, CachedLinkEntry.NotFound, NegativeTtl, cancellationToken);
            return LinkResolution.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        if (link.ExpiresAt is { } expiresAt && expiresAt <= now)
        {
            await cache.SetAsync(code, CachedLinkEntry.Expired, NegativeTtl, cancellationToken);
            return LinkResolution.Expired();
        }

        var ttl = ComputeFoundTtl(link.ExpiresAt, now);
        await cache.SetAsync(code, CachedLinkEntry.Found(link.Url), ttl, cancellationToken);
        return LinkResolution.Found(new Uri(link.Url));
    }

    /// <summary>
    /// The cache duration for a found link: the time remaining until its
    /// expiry, capped at <see cref="MaxFoundTtl"/>, or the cap itself for a
    /// link with no expiry.
    /// </summary>
    /// <param name="expiresAt">The link's expiry, or <see langword="null"/> if it never expires.</param>
    /// <param name="now">The current instant.</param>
    /// <returns>How long the found result should be cached.</returns>
    internal static TimeSpan ComputeFoundTtl(DateTimeOffset? expiresAt, DateTimeOffset now) =>
        expiresAt is { } value && value - now < MaxFoundTtl ? value - now : MaxFoundTtl;

    private static LinkResolution ToResolution(CachedLinkEntry entry) => entry.Status switch
    {
        CachedLinkStatus.Found => LinkResolution.Found(new Uri(entry.DestinationUrl!)),
        CachedLinkStatus.Expired => LinkResolution.Expired(),
        CachedLinkStatus.NotFound => LinkResolution.NotFound(),
        _ => throw new InvalidOperationException($"Unknown cached status: {entry.Status}"),
    };
}
