using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Handles the get-link-by-code use case.</summary>
public sealed class GetLinkHandler(ILinkRepository repository)
{
    /// <summary>Looks up a link by its short code.</summary>
    /// <param name="code">
    /// The short code to look up. A malformed code (wrong length or
    /// alphabet) can never match a link, so it is treated the same as an
    /// unmatched well-formed one: not found.
    /// </param>
    /// <param name="callerId">
    /// The authenticated user's identity, or <see langword="null"/> for
    /// direct (unauthenticated) calls. When set, links owned by a
    /// different user appear as not found.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the lookup.</returns>
    public async Task<GetLinkResult> HandleAsync(string code, string? callerId, CancellationToken cancellationToken)
    {
        var codeResult = ShortCode.Create(code);
        if (!codeResult.IsValid)
        {
            return GetLinkResult.NotFound();
        }

        var link = await repository.GetByCodeAsync(codeResult.Value!, cancellationToken);
        if (link is null)
        {
            return GetLinkResult.NotFound();
        }

        if (callerId is not null && link.OwnerId is not null && link.OwnerId != callerId)
        {
            return GetLinkResult.NotFound();
        }

        return GetLinkResult.Found(link);
    }
}
