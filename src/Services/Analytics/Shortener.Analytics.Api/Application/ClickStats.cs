namespace Shortener.Analytics.Api.Application;

/// <summary>Click count for a single day, as part of a code's stats.</summary>
/// <param name="Date">The day the clicks occurred (UTC).</param>
/// <param name="Count">How many clicks occurred that day.</param>
public sealed record DailyClicks(DateOnly Date, int Count);

/// <summary>Click stats for a short code.</summary>
/// <param name="Code">The short code.</param>
/// <param name="TotalClicks">The total clicks across all days.</param>
/// <param name="DailyClicks">Per-day click counts, ordered by date.</param>
public sealed record ClickStats(string Code, int TotalClicks, IReadOnlyList<DailyClicks> DailyClicks);
