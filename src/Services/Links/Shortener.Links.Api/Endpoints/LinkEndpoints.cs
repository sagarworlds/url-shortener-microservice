using System.Diagnostics;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Endpoints;

/// <summary>Route mapping for link creation, lookup and deletion.</summary>
public static class LinkEndpoints
{
    /// <summary>Maps <c>POST/GET/DELETE /links</c>.</summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/links", CreateLinkAsync);
        endpoints.MapGet("/links/{code}", GetLinkAsync);
        endpoints.MapDelete("/links/{code}", DeleteLinkAsync);
        endpoints.MapPost("/links/backfill", BackfillAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateLinkAsync(
        CreateLinkBody body,
        HttpRequest httpRequest,
        CreateLinkHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new CreateLinkRequest(body.Url, body.Alias, body.ExpiresAt);
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Status switch
        {
            CreateLinkStatus.Created => Results.Created(
                $"/links/{result.Link!.Code.Value}",
                ToResponse(result.Link, httpRequest)),
            CreateLinkStatus.Invalid => Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid link request"),
            CreateLinkStatus.AliasTaken => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Alias already in use"),
            _ => throw new UnreachableException($"Unhandled {nameof(CreateLinkStatus)}: {result.Status}"),
        };
    }

    private static async Task<IResult> GetLinkAsync(
        string code,
        HttpRequest httpRequest,
        GetLinkHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(code, cancellationToken);

        return result.IsFound
            ? Results.Ok(ToResponse(result.Link!, httpRequest))
            : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Link not found");
    }

    private static async Task<IResult> DeleteLinkAsync(
        string code,
        DeleteLinkHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(code, cancellationToken);

        return result.WasDeleted
            ? Results.NoContent()
            : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Link not found");
    }

    private static async Task<IResult> BackfillAsync(BackfillOutboxHandler handler, CancellationToken cancellationToken)
    {
        var count = await handler.HandleAsync(cancellationToken);
        return Results.Ok(new BackfillResponse(count));
    }

    // The Location header (set by Results.Created above) points at this
    // Links API's own GET /links/{code} resource. ShortUrl is the public,
    // user-facing redirect address; until M4's gateway gives every service
    // one public host, it's built from whatever host the client used to
    // reach Links.
    private static LinkResponse ToResponse(Link link, HttpRequest request) => new(
        link.Code.Value,
        $"{request.Scheme}://{request.Host}/{link.Code.Value}",
        link.DestinationUrl.Value.ToString(),
        link.ExpiresAt);
}

/// <summary>Request body for <c>POST /links</c>.</summary>
/// <param name="Url">The destination URL to shorten.</param>
/// <param name="Alias">An optional caller-chosen short code.</param>
/// <param name="ExpiresAt">When the link should stop resolving, or <see langword="null"/> for no expiry.</param>
public sealed record CreateLinkBody(string Url, string? Alias, DateTimeOffset? ExpiresAt);

/// <summary>Response body for a link, returned by create and get.</summary>
/// <param name="Code">The short code.</param>
/// <param name="ShortUrl">The public URL that redirects to <paramref name="Url"/>.</param>
/// <param name="Url">The destination URL.</param>
/// <param name="ExpiresAt">When the link stops resolving, or <see langword="null"/> if it never expires.</param>
public sealed record LinkResponse(string Code, string ShortUrl, string Url, DateTimeOffset? ExpiresAt);

/// <summary>Response body for <c>POST /links/backfill</c>.</summary>
/// <param name="Backfilled">How many links were backfilled.</param>
public sealed record BackfillResponse(int Backfilled);
