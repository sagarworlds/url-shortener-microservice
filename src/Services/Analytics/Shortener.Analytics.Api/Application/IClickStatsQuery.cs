namespace Shortener.Analytics.Api.Application;

/// <summary>Reads recorded click stats.</summary>
public interface IClickStatsQuery
{
    /// <summary>
    /// Gets click stats for a code. Analytics has no way to know which codes
    /// are real Links codes without crossing the database-per-service
    /// boundary, so a code with no recorded clicks simply comes back with
    /// zeros rather than a not-found outcome.
    /// </summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The code's click stats.</returns>
    Task<ClickStats> GetStatsAsync(string code, CancellationToken cancellationToken);
}
