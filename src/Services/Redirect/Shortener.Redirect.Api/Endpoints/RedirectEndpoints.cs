using System.Diagnostics;
using Shortener.Contracts;
using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Endpoints;

/// <summary>Route mapping for the redirect hot path.</summary>
public static class RedirectEndpoints
{
    /// <summary>Maps <c>GET /{code}</c>.</summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{code}", RedirectAsync);

        return endpoints;
    }

    private static async Task<IResult> RedirectAsync(
        string code,
        HttpRequest httpRequest,
        ILinkResolver resolver,
        IClickPublisher clickPublisher,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var result = await resolver.ResolveAsync(code, cancellationToken);

        return result.Status switch
        {
            // 302, never 301: a permanent redirect would let browsers cache
            // it forever, hiding clicks from analytics and making expiry and
            // deletion unenforceable.
            LinkResolutionStatus.Found => PublishClickAndRedirect(code, httpRequest, clickPublisher, timeProvider, result),
            LinkResolutionStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Link not found"),
            LinkResolutionStatus.Expired => Results.Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Link has expired"),
            LinkResolutionStatus.Unavailable => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Links is temporarily unavailable"),
            _ => throw new UnreachableException($"Unhandled {nameof(LinkResolutionStatus)}: {result.Status}"),
        };
    }

    private static IResult PublishClickAndRedirect(
        string code,
        HttpRequest httpRequest,
        IClickPublisher clickPublisher,
        TimeProvider timeProvider,
        LinkResolution result)
    {
        var referrerHeader = httpRequest.Headers.Referer;
        var referrer = referrerHeader.Count > 0 ? referrerHeader.ToString() : null;

        clickPublisher.Publish(new LinkClicked(Guid.NewGuid(), code, timeProvider.GetUtcNow(), referrer));

        return Results.Redirect(result.Destination!.ToString(), permanent: false);
    }
}
