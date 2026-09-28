using Shortener.Redirect.Api.Application;
using StackExchange.Redis;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Resolves short codes from Redirect's own Redis read model, fed by
/// Links' outbox events via <c>LinkEventConsumer</c> — no HTTP call to
/// Links at all. Replaces M1's <c>CacheAsideLinkResolver</c> as the active
/// <c>ILinkResolver</c>; that implementation is left in place, unused, since
/// swapping between them is meant to be a one-line DI change.
/// </summary>
/// <remarks>
/// Unlike the cache-aside resolver, a miss here can only ever mean
/// not-found: once Redis is the sole source of truth, there's no fallback
/// left to ask whether a miss is "never existed" or "existed, then
/// expired," so this never produces <see cref="LinkResolutionStatus.Expired"/>.
/// </remarks>
internal sealed class ReadModelLinkResolver(IReadModelStore readModelStore, ILogger<ReadModelLinkResolver> logger) : ILinkResolver
{
    /// <inheritdoc />
    public async Task<LinkResolution> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var destinationUrl = await readModelStore.GetAsync(code, cancellationToken);
            return destinationUrl is null
                ? LinkResolution.NotFound()
                : LinkResolution.Found(new Uri(destinationUrl));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            // Redis is now Redirect's only dependency, so its unavailability
            // gets the same treatment M1 gave a Links outage: 503, never a
            // false 404.
            logger.LogWarning(ex, "Redis unreachable resolving code {Code}", code);
            return LinkResolution.Unavailable();
        }
    }
}
