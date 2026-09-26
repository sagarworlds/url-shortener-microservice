using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;

namespace Shortener.IntegrationTests;

/// <summary>
/// Exercises the whole click pipeline: Redirect publishes a LinkClicked
/// event over RabbitMQ, Analytics consumes it and updates its stats.
/// Matches M2's "clicks appear in GET /analytics/{code} within seconds"
/// done-when item. Requires Docker running locally.
/// </summary>
public class ClickAnalyticsEndToEndTests
{
    private sealed record CreatedLink(string Code, string ShortUrl, string Url, DateTimeOffset? ExpiresAt);

    private sealed record DailyClicksResponse(DateOnly Date, int Count);

    private sealed record AnalyticsResponse(string Code, int TotalClicks, IReadOnlyList<DailyClicksResponse> DailyClicks);

    [Fact]
    public async Task GetAnalytics_AfterRedirectClick_ShowsOneClickWithinSeconds()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shortener_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("links", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("redirect", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("messaging", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("analytics", cancellationToken);

        const string destination = "https://example.com/click-target";

        using var linksHttpClient = app.CreateHttpClient("links");
        using var createResponse = await linksHttpClient.PostAsJsonAsync(
            "/links",
            new { url = destination },
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedLink>(cancellationToken);
        Assert.NotNull(created);

        using var redirectHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = app.GetEndpoint("redirect", "http"),
        };
        using var redirectResponse = await redirectHttpClient.GetAsync($"/{created!.Code}", cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

        using var analyticsHttpClient = app.CreateHttpClient("analytics");
        var stats = await PollForClickAsync(analyticsHttpClient, created.Code, cancellationToken);

        Assert.NotNull(stats);
        Assert.Equal(created.Code, stats!.Code);
        Assert.Equal(1, stats.TotalClicks);
    }

    private static async Task<AnalyticsResponse?> PollForClickAsync(
        HttpClient analyticsHttpClient,
        string code,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var statsResponse = await analyticsHttpClient.GetAsync($"/analytics/{code}", cancellationToken);
            statsResponse.EnsureSuccessStatusCode();

            var stats = await statsResponse.Content.ReadFromJsonAsync<AnalyticsResponse>(cancellationToken);
            if (stats is { TotalClicks: >= 1 })
            {
                return stats;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return null;
    }
}
