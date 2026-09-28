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
    /// <param name="callerId">
    /// The authenticated user's identity, or <see langword="null"/> for
    /// direct (unauthenticated) calls. When set, only links owned by
    /// this user (or unowned links) can be deleted.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the deletion.</returns>
    public async Task<DeleteLinkResult> HandleAsync(string code, string? callerId, CancellationToken cancellationToken)
    {
        var codeResult = ShortCode.Create(code);
        if (!codeResult.IsValid)
        {
            return DeleteLinkResult.NotFound();
        }

        var link = await repository.GetByCodeAsync(codeResult.Value!, cancellationToken);
        if (link is null)
        {
            return DeleteLinkResult.NotFound();
        }

        if (callerId is not null && link.OwnerId is not null && link.OwnerId != callerId)
        {
            return DeleteLinkResult.NotFound();
        }

        var deletedEvent = new LinkDeleted(Guid.NewGuid(), codeResult.Value!.Value, timeProvider.GetUtcNow());
        var deleted = await repository.DeleteByCodeAsync(codeResult.Value!, deletedEvent, cancellationToken);
        return deleted ? DeleteLinkResult.Deleted() : DeleteLinkResult.NotFound();
    }
}
