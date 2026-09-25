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
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the lookup.</returns>
    public async Task<GetLinkResult> HandleAsync(string code, CancellationToken cancellationToken)
    {
        var codeResult = ShortCode.Create(code);
        if (!codeResult.IsValid)
        {
            return GetLinkResult.NotFound();
        }

        var link = await repository.GetByCodeAsync(codeResult.Value!, cancellationToken);
        return link is null ? GetLinkResult.NotFound() : GetLinkResult.Found(link);
    }
}
