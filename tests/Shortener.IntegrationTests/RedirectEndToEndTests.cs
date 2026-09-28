using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting.Testing;

namespace Shortener.IntegrationTests;

/// <summary>
/// Exercises Links and Redirect together through the real AppHost graph
/// (Postgres, Redis, RabbitMQ, both services). From M3 onward, Redirect
/// resolves codes from an event-fed Redis read model rather than calling
/// Links over HTTP, so the test polls until the outbox-dispatcher →
/// RabbitMQ → consumer pipeline propagates the link. Requires Docker.
/// </summary>
public class RedirectEndToEndTests
{
    [Fact]
    public async Task GetRedirect_ForLinkCreatedThroughLinks_Returns302WithDestinationLocation()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shortener_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("links", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("redirect", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("messaging", cancellationToken);

        const string destination = "https://example.com/target";

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

        using var redirectResponse = await PollForRedirectAsync(redirectHttpClient, created!.Code, cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(new Uri(destination), redirectResponse.Headers.Location);
    }

    /// <summary>
    /// Polls until the read-model pipeline propagates the link (outbox
    /// dispatcher → RabbitMQ → consumer → Redis).
    /// </summary>
    private static async Task<HttpResponseMessage> PollForRedirectAsync(
        HttpClient redirectHttpClient,
        string code,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await redirectHttpClient.GetAsync($"/{code}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"Code '{code}' never resolved through the read model.");
    }

    private sealed record CreatedLink(string Code, string ShortUrl, string Url, DateTimeOffset? ExpiresAt);
}
