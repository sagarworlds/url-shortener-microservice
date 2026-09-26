using Shortener.Redirect.Api.Application;
using StackExchange.Redis;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Redis-backed <see cref="IReadModelStore"/>. Uses the
/// <c>readmodel:link:</c> key prefix, distinct from M1's <c>link:</c> cache
/// prefix — the read model stores a bare destination-URL string, not the
/// JSON-enveloped shape the M1 cache used, so reusing the same prefix could
/// make a leftover M1 entry misread as a read-model one.
/// </summary>
/// <param name="redis">The Redis connection, provided by Aspire's <c>AddRedisClient</c>.</param>
internal sealed class RedisReadModelStore(IConnectionMultiplexer redis) : IReadModelStore
{
    /// <inheritdoc />
    public Task SetAsync(string code, string destinationUrl, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        Expiration expiration = ttl is { } value ? value : Expiration.Default;
        return redis.GetDatabase().StringSetAsync(CacheKey(code), destinationUrl, expiration);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string code, CancellationToken cancellationToken)
    {
        var value = await redis.GetDatabase().StringGetAsync(CacheKey(code));
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string code, CancellationToken cancellationToken) =>
        redis.GetDatabase().KeyDeleteAsync(CacheKey(code));

    private static string CacheKey(string code) => $"readmodel:link:{code}";
}
