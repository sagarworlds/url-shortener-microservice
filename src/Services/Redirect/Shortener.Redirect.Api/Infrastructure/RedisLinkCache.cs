using System.Text.Json;
using Shortener.Redirect.Api.Application;
using StackExchange.Redis;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>Redis-backed <see cref="ILinkCache"/>.</summary>
/// <param name="redis">The Redis connection, provided by Aspire's <c>AddRedisClient</c>.</param>
internal sealed class RedisLinkCache(IConnectionMultiplexer redis) : ILinkCache
{
    /// <inheritdoc />
    public async Task<CachedLinkEntry?> GetAsync(string code, CancellationToken cancellationToken)
    {
        // StackExchange.Redis's IDatabase API has no cancellation support;
        // cancellationToken is accepted only to satisfy ILinkCache's contract.
        var value = await redis.GetDatabase().StringGetAsync(CacheKey(code));
        return value.HasValue ? Deserialize(value!) : null;
    }

    /// <inheritdoc />
    public Task SetAsync(string code, CachedLinkEntry entry, TimeSpan ttl, CancellationToken cancellationToken) =>
        redis.GetDatabase().StringSetAsync(CacheKey(code), Serialize(entry), ttl);

    private static string CacheKey(string code) => $"link:{code}";

    private static string Serialize(CachedLinkEntry entry) => JsonSerializer.Serialize(entry);

    private static CachedLinkEntry Deserialize(string value) =>
        JsonSerializer.Deserialize<CachedLinkEntry>(value)
            ?? throw new InvalidOperationException($"Corrupt cache entry: '{value}'.");
}
