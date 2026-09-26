using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shortener.Redirect.Api.Application;
using StackExchange.Redis;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Resolves short codes by checking Redis first and falling back to Links
/// over HTTP on a miss. Used until M3 replaces it with a
/// <c>ReadModelLinkResolver</c> fed by <c>LinkCreated</c>/<c>LinkDeleted</c>
/// events instead of a direct HTTP dependency on Links.
/// </summary>
public sealed class CacheAsideLinkResolver(
    IConnectionMultiplexer redis,
    LinksClient linksClient,
    TimeProvider timeProvider,
    ILogger<CacheAsideLinkResolver> logger) : ILinkResolver
{
    /// <summary>The cap on how long a found link is cached, regardless of how far away its expiry is.</summary>
    internal static readonly TimeSpan MaxFoundTtl = TimeSpan.FromHours(24);

    /// <summary>How long a not-found or expired outcome is cached, to absorb repeat lookups without hammering Links.</summary>
    internal static readonly TimeSpan NegativeTtl = TimeSpan.FromSeconds(60);

    private static string CacheKey(string code) => $"link:{code}";

    /// <inheritdoc />
    public async Task<LinkResolution> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        var database = redis.GetDatabase();
        var key = CacheKey(code);

        var cached = await database.StringGetAsync(key);
        if (cached.HasValue)
        {
            return ToResolution(Deserialize(cached!));
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
            await database.StringSetAsync(key, Serialize(CachedLink.NotFound), NegativeTtl);
            return LinkResolution.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        if (link.ExpiresAt is { } expiresAt && expiresAt <= now)
        {
            await database.StringSetAsync(key, Serialize(CachedLink.Expired), NegativeTtl);
            return LinkResolution.Expired();
        }

        var ttl = link.ExpiresAt is { } notExpiredAt ? Min(MaxFoundTtl, notExpiredAt - now) : MaxFoundTtl;
        await database.StringSetAsync(key, Serialize(CachedLink.Found(link.Url)), ttl);
        return LinkResolution.Found(new Uri(link.Url));
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

    private static string Serialize(CachedLink entry) => JsonSerializer.Serialize(entry);

    private static CachedLink Deserialize(string value) =>
        JsonSerializer.Deserialize<CachedLink>(value)
            ?? throw new InvalidOperationException($"Corrupt cache entry: '{value}'.");

    private static LinkResolution ToResolution(CachedLink entry) => entry.Status switch
    {
        CachedLinkStatus.Found => LinkResolution.Found(new Uri(entry.DestinationUrl!)),
        CachedLinkStatus.Expired => LinkResolution.Expired(),
        CachedLinkStatus.NotFound => LinkResolution.NotFound(),
        _ => throw new InvalidOperationException($"Unknown cached status: {entry.Status}"),
    };
}
