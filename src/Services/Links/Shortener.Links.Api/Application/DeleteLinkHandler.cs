using Shortener.Contracts;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Handles the delete-link-by-code use case.</summary>
public sealed class DeleteLinkHandler(ILinkRepository repository, TimeProvider timeProvider)
{
    /// <summary>Deletes a link by its short code.</summary>
    /// <param name="code">
    /// The short code to delete. A malformed code can never match a link,
    /// so it is treated the same as an unmatched well-formed one: not found.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the deletion.</returns>
    public async Task<DeleteLinkResult> HandleAsync(string code, CancellationToken cancellationToken)
    {
        var codeResult = ShortCode.Create(code);
        if (!codeResult.IsValid)
        {
            return DeleteLinkResult.NotFound();
        }

        var deletedEvent = new LinkDeleted(Guid.NewGuid(), codeResult.Value!.Value, timeProvider.GetUtcNow());
        var deleted = await repository.DeleteByCodeAsync(codeResult.Value!, deletedEvent, cancellationToken);
        return deleted ? DeleteLinkResult.Deleted() : DeleteLinkResult.NotFound();
    }
}
