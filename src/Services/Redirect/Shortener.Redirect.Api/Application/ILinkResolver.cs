namespace Shortener.Redirect.Api.Application;

/// <summary>
/// Resolves a short code to a redirect destination. This is the seam that
/// swaps from a cache-aside HTTP lookup (M1) to an event-fed read model
/// (M3) without any change to the endpoint that calls it.
/// </summary>
public interface ILinkResolver
{
    /// <summary>Resolves a short code.</summary>
    /// <param name="code">The short code from the request path.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the resolution.</returns>
    Task<LinkResolution> ResolveAsync(string code, CancellationToken cancellationToken);
}
