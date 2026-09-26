namespace Shortener.Redirect.Api.Application;

/// <summary>Cache of previously resolved short codes.</summary>
internal interface ILinkCache
{
    /// <summary>Looks up a cached resolution for a code.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The cached entry, or <see langword="null"/> on a cache miss.</returns>
    Task<CachedLinkEntry?> GetAsync(string code, CancellationToken cancellationToken);

    /// <summary>Caches a resolution for a code.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="entry">The outcome to cache.</param>
    /// <param name="ttl">How long the entry should live.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SetAsync(string code, CachedLinkEntry entry, TimeSpan ttl, CancellationToken cancellationToken);
}
