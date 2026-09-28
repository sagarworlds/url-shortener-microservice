namespace Shortener.Analytics.Api.Application;

/// <summary>Reads link ownership data projected from <c>LinkCreated</c> events.</summary>
public interface ILinkOwnerQuery
{
    /// <summary>Gets the owner of a link by its short code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>
    /// The owner's identity, or <see langword="null"/> if the code has no
    /// known owner (either the link predates auth or no event has arrived yet).
    /// </returns>
    Task<string?> GetOwnerAsync(string code, CancellationToken cancellationToken);
}
