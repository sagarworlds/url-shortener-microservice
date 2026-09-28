namespace Shortener.Analytics.Api.Application;

/// <summary>Handles the get-click-stats use case.</summary>
public sealed class GetClickStatsHandler(IClickStatsQuery query, ILinkOwnerQuery linkOwnerQuery)
{
    /// <summary>Gets click stats for a code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="callerId">
    /// The authenticated user's identity, or <see langword="null"/> for
    /// direct (unauthenticated) calls. When set, stats for links owned by
    /// a different user appear as not found.
    /// </param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The outcome of the lookup.</returns>
    public async Task<GetClickStatsResult> HandleAsync(string code, string? callerId, CancellationToken cancellationToken)
    {
        var ownerId = await linkOwnerQuery.GetOwnerAsync(code, cancellationToken);

        if (callerId is not null && ownerId is not null && ownerId != callerId)
        {
            return GetClickStatsResult.NotFound();
        }

        var stats = await query.GetStatsAsync(code, cancellationToken);
        return GetClickStatsResult.Found(stats);
    }
}
