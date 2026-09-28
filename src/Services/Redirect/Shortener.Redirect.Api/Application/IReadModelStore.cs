namespace Shortener.Redirect.Api.Application;

/// <summary>
/// The Redis-backed read model fed by Links' outbox events. Unlike M1's
/// cache-aside <c>ILinkCache</c>, this is Redirect's only source of truth
/// once <c>ReadModelLinkResolver</c> is active — there's no Links to fall
/// back to on a miss.
/// </summary>
internal interface IReadModelStore
{
    /// <summary>Sets the destination URL for a code.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="destinationUrl">The destination URL.</param>
    /// <param name="ttl">
    /// How long the entry should live, matching the link's time until
    /// expiry, or <see langword="null"/> for a link that never expires.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SetAsync(string code, string destinationUrl, TimeSpan? ttl, CancellationToken cancellationToken);

    /// <summary>Looks up the destination URL for a code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The destination URL, or <see langword="null"/> if the code isn't in the read model.</returns>
    Task<string?> GetAsync(string code, CancellationToken cancellationToken);

    /// <summary>Removes a code from the read model.</summary>
    /// <param name="code">The short code to remove.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task DeleteAsync(string code, CancellationToken cancellationToken);
}
