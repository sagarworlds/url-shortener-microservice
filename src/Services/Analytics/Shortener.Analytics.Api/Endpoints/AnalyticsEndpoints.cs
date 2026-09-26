using Shortener.Analytics.Api.Application;

namespace Shortener.Analytics.Api.Endpoints;

/// <summary>Route mapping for reading click stats.</summary>
public static class AnalyticsEndpoints
{
    /// <summary>Maps <c>GET /analytics/{code}</c>.</summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/analytics/{code}", GetAnalyticsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAnalyticsAsync(
        string code,
        GetClickStatsHandler handler,
        CancellationToken cancellationToken)
    {
        var stats = await handler.HandleAsync(code, cancellationToken);
        return Results.Ok(ToResponse(stats));
    }

    private static AnalyticsResponse ToResponse(ClickStats stats) => new(
        stats.Code,
        stats.TotalClicks,
        stats.DailyClicks.Select(day => new DailyClicksResponse(day.Date, day.Count)).ToList());
}

/// <summary>Per-day click count in an <see cref="AnalyticsResponse"/>.</summary>
/// <param name="Date">The day the clicks occurred (UTC).</param>
/// <param name="Count">How many clicks occurred that day.</param>
public sealed record DailyClicksResponse(DateOnly Date, int Count);

/// <summary>Response body for <c>GET /analytics/{code}</c>.</summary>
/// <param name="Code">The short code.</param>
/// <param name="TotalClicks">The total clicks across all days.</param>
/// <param name="DailyClicks">Per-day click counts, ordered by date.</param>
public sealed record AnalyticsResponse(string Code, int TotalClicks, IReadOnlyList<DailyClicksResponse> DailyClicks);
