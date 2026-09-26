using System.Net;
using System.Net.Http.Json;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>Typed HTTP client for the Links API's <c>GET /links/{code}</c> endpoint.</summary>
/// <param name="httpClient">
/// An <see cref="HttpClient"/> whose base address resolves <c>links</c> through
/// Aspire service discovery, with standard resilience already applied by
/// Shortener.ServiceDefaults.
/// </param>
public sealed class LinksClient(HttpClient httpClient)
{
    /// <summary>Looks up a link by its short code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The link's details, or <see langword="null"/> if Links has no link with that code.</returns>
    /// <exception cref="HttpRequestException">Links returned a non-success status other than 404, or could not be reached after retries.</exception>
    public async Task<LinkDetails?> GetLinkAsync(string code, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/links/{code}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LinkDetails>(cancellationToken);
    }
}

/// <summary>The subset of a Links API link representation that Redirect needs.</summary>
/// <param name="Code">The short code.</param>
/// <param name="Url">The destination URL.</param>
/// <param name="ExpiresAt">When the link stops resolving, or <see langword="null"/> if it never expires.</param>
public sealed record LinkDetails(string Code, string Url, DateTimeOffset? ExpiresAt);
