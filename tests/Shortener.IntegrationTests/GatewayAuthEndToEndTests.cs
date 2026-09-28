using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Shortener.IntegrationTests;

/// <summary>
/// Proves the four M4 "Done when" criteria end-to-end through the YARP
/// gateway backed by Keycloak auth and rate limiting. Shares a single
/// AppHost across all tests via <see cref="GatewayFixture"/> to avoid
/// re-starting Keycloak for every test. Requires Docker running locally.
/// </summary>
public class GatewayAuthEndToEndTests(GatewayFixture fixture) : IClassFixture<GatewayFixture>
{
    private sealed record CreatedLink(string Code, string ShortUrl, string Url, DateTimeOffset? ExpiresAt);

    [Fact]
    public async Task PostLinks_WithoutToken_Returns401()
    {
        using var gatewayClient = fixture.App.CreateHttpClient("gateway");

        using var response = await gatewayClient.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/no-auth" },
            fixture.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRedirect_Anonymous_Returns302()
    {
        using var linksClient = fixture.App.CreateHttpClient("links");
        using var createResponse = await linksClient.PostAsJsonAsync(
            "/links",
            new { url = "https://example.com/anon-redirect" },
            fixture.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedLink>(fixture.CancellationToken);
        Assert.NotNull(created);

        // Wait for the read model to be populated before hitting the gateway.
        using var redirectClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = fixture.App.GetEndpoint("redirect", "http"),
        };
        await PollForRedirectAsync(redirectClient, created!.Code, fixture.CancellationToken);

        // Now test the anonymous redirect through the gateway.
        using var gatewayRedirectClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = fixture.App.GetEndpoint("gateway", "http"),
        };
        using var gwResponse = await gatewayRedirectClient.GetAsync($"/{created.Code}", fixture.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, gwResponse.StatusCode);
        Assert.Equal(new Uri("https://example.com/anon-redirect"), gwResponse.Headers.Location);
    }

    [Fact]
    public async Task GetAnalytics_ForOtherUsersLink_Returns404()
    {
        var aliceToken = await fixture.GetTokenAsync("alice", "alice");
        var bobToken = await fixture.GetTokenAsync("bob", "bob");

        // Alice creates a link through the gateway.
        using var gatewayClient = fixture.App.CreateHttpClient("gateway");
        gatewayClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);

        using var createResponse = await gatewayClient.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/alice-only" },
            fixture.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedLink>(fixture.CancellationToken);
        Assert.NotNull(created);

        // Poll until Bob gets 404 (ownership event has propagated).
        using var bobClient = fixture.App.CreateHttpClient("gateway");
        bobClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);

        var gotNotFound = false;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            using var statsResponse = await bobClient.GetAsync(
                $"/api/analytics/{created!.Code}", fixture.CancellationToken);
            if (statsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                gotNotFound = true;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), fixture.CancellationToken);
        }

        Assert.True(gotNotFound, "Bob should get 404 for Alice's link analytics.");
    }

    [Fact]
    public async Task PostLinks_BurstExceedsLimit_Returns429()
    {
        var aliceToken = await fixture.GetTokenAsync("alice", "alice");

        using var gatewayClient = fixture.App.CreateHttpClient("gateway");
        gatewayClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);

        var statusCodes = new List<HttpStatusCode>();
        for (var i = 0; i < 15; i++)
        {
            using var response = await gatewayClient.PostAsJsonAsync(
                "/api/links",
                new { url = $"https://example.com/rate-{Guid.NewGuid()}" },
                fixture.CancellationToken);
            statusCodes.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statusCodes);
    }

    private static async Task PollForRedirectAsync(
        HttpClient redirectClient,
        string code,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await redirectClient.GetAsync($"/{code}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"Code '{code}' never resolved through the read model.");
    }
}

/// <summary>
/// Shared fixture that starts the Aspire AppHost once for all gateway
/// integration tests. Waits for all resources (including Keycloak) to be
/// ready before tests run.
/// </summary>
public sealed class GatewayFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private readonly CancellationTokenSource _timeout = new(TimeSpan.FromMinutes(10));

    /// <summary>The running Aspire app.</summary>
    public DistributedApplication App => _app;

    /// <summary>Cancellation token tied to the fixture's overall timeout.</summary>
    public CancellationToken CancellationToken => _timeout.Token;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shortener_AppHost>(CancellationToken);
        _app = await appHost.BuildAsync(CancellationToken);
        await _app.StartAsync(CancellationToken);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("links", CancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("redirect", CancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("analytics", CancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("messaging", CancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("gateway", CancellationToken);

        await WaitForKeycloakAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        _timeout.Dispose();
    }

    /// <summary>Acquires a JWT from Keycloak using the password grant.</summary>
    /// <param name="username">The Keycloak user.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>The access token string.</returns>
    public async Task<string> GetTokenAsync(string username, string password)
    {
        var keycloakEndpoint = _app.GetEndpoint("keycloak", "http");
        var tokenUrl = $"{keycloakEndpoint}realms/shortener/protocol/openid-connect/token";

        using var httpClient = new HttpClient();
        using var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "shortener-api",
            ["username"] = username,
            ["password"] = password,
        }), CancellationToken);

        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(CancellationToken);
        return tokenResponse!.AccessToken;
    }

    private async Task WaitForKeycloakAsync()
    {
        var keycloakEndpoint = _app.GetEndpoint("keycloak", "http");
        var discoveryUrl = $"{keycloakEndpoint}realms/shortener/.well-known/openid-configuration";

        using var httpClient = new HttpClient();
        for (var attempt = 0; attempt < 120; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(discoveryUrl, CancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Keycloak is still starting.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
        }

        throw new TimeoutException("Keycloak did not become ready within the timeout.");
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = "";
    }
}
