namespace Shortener.Analytics.Api.Application;

/// <summary>Writes link ownership data projected from link lifecycle events.</summary>
public interface ILinkOwnerStore
{
    /// <summary>Records the owner of a newly created link.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="ownerId">The owner's identity.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SetOwnerAsync(string code, string ownerId, CancellationToken cancellationToken);

    /// <summary>Removes the ownership record when a link is deleted.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task RemoveOwnerAsync(string code, CancellationToken cancellationToken);
}
