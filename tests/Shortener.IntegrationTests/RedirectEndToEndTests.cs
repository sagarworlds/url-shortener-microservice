using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;

namespace Shortener.IntegrationTests;

/// <summary>
/// Exercises Links and Redirect together through the real AppHost graph
/// (Postgres, Redis, both services), matching M1's "Done when" checklist
/// in docs/roadmap.md. Requires Docker running locally.
/// </summary>
public class RedirectEndToEndTests
{
    [Fact]
    public async Task GetRedirect_ForLinkCreatedThroughLinks_Returns302WithDestinationLocation()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var cancellationToken = timeout.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shortener_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("links", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("redirect", cancellationToken);

        const string destination = "https://example.com/target";

        using var linksHttpClient = app.CreateHttpClient("links");
        using var createResponse = await linksHttpClient.PostAsJsonAsync(
            "/links",
            new { url = destination },
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedLink>(cancellationToken);
        Assert.NotNull(created);

        // A client with auto-redirect on would silently follow straight to
        // the destination, hiding the 302/Location contract this test exists
        // to check.
        using var redirectHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = app.GetEndpoint("redirect", "http"),
        };

        using var redirectResponse = await redirectHttpClient.GetAsync($"/{created!.Code}", cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(new Uri(destination), redirectResponse.Headers.Location);
    }

    private sealed record CreatedLink(string Code, string ShortUrl, string Url, DateTimeOffset? ExpiresAt);
}
