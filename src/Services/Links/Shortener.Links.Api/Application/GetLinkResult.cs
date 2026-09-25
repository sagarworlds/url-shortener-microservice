using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Result of <see cref="GetLinkHandler"/>.</summary>
public sealed class GetLinkResult
{
    private GetLinkResult(Link? link) => Link = link;

    /// <summary>The matching link, or <see langword="null"/> if none was found.</summary>
    public Link? Link { get; }

    /// <summary>Whether a link was found.</summary>
    public bool IsFound => Link is not null;

    /// <summary>Creates a result for a found link.</summary>
    /// <param name="link">The link that was found.</param>
    /// <returns>A result wrapping <paramref name="link"/>.</returns>
    public static GetLinkResult Found(Link link) => new(link);

    /// <summary>Creates a result for no matching link.</summary>
    /// <returns>A result with no link.</returns>
    public static GetLinkResult NotFound() => new(null);
}
