namespace Shortener.Analytics.Api.Application;

/// <summary>Handles the get-click-stats use case.</summary>
public sealed class GetClickStatsHandler(IClickStatsQuery query)
{
    /// <summary>Gets click stats for a code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The code's click stats.</returns>
    public Task<ClickStats> HandleAsync(string code, CancellationToken cancellationToken) =>
        query.GetStatsAsync(code, cancellationToken);
}
