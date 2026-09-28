namespace Shortener.Analytics.Api.Application;

/// <summary>Result of <see cref="GetClickStatsHandler"/>.</summary>
public sealed class GetClickStatsResult
{
    private GetClickStatsResult(bool isFound, ClickStats? stats)
    {
        IsFound = isFound;
        Stats = stats;
    }

    /// <summary>Whether the caller is allowed to see the stats.</summary>
    public bool IsFound { get; }

    /// <summary>The click stats, or <see langword="null"/> when <see cref="IsFound"/> is <see langword="false"/>.</summary>
    public ClickStats? Stats { get; }

    /// <summary>Creates a result for an authorized caller.</summary>
    /// <param name="stats">The click stats.</param>
    /// <returns>A result with <see cref="IsFound"/> set to <see langword="true"/>.</returns>
    public static GetClickStatsResult Found(ClickStats stats) => new(true, stats);

    /// <summary>Creates a result when the caller is not the owner.</summary>
    /// <returns>A result with <see cref="IsFound"/> set to <see langword="false"/>.</returns>
    public static GetClickStatsResult NotFound() => new(false, null);
}
