namespace Shortener.Redirect.Api.Application;

/// <summary>The possible outcomes of <see cref="ILinkResolver.ResolveAsync"/>.</summary>
public enum LinkResolutionStatus
{
    /// <summary>The code resolved to a live destination.</summary>
    Found,

    /// <summary>No link has this code.</summary>
    NotFound,

    /// <summary>The link exists but has passed its expiry.</summary>
    Expired,

    /// <summary>Links could not be reached and the answer is unknown.</summary>
    Unavailable,
}

/// <summary>Result of resolving a short code.</summary>
public sealed class LinkResolution
{
    private LinkResolution(LinkResolutionStatus status, Uri? destination)
    {
        Status = status;
        Destination = destination;
    }

    /// <summary>Which of the possible outcomes occurred.</summary>
    public LinkResolutionStatus Status { get; }

    /// <summary>The destination URL when <see cref="Status"/> is <see cref="LinkResolutionStatus.Found"/>; otherwise <see langword="null"/>.</summary>
    public Uri? Destination { get; }

    /// <summary>Creates a <see cref="LinkResolutionStatus.Found"/> result.</summary>
    /// <param name="destination">The URL to redirect to.</param>
    /// <returns>A result wrapping <paramref name="destination"/>.</returns>
    public static LinkResolution Found(Uri destination) => new(LinkResolutionStatus.Found, destination);

    /// <summary>Creates a <see cref="LinkResolutionStatus.NotFound"/> result.</summary>
    /// <returns>A result with no destination.</returns>
    public static LinkResolution NotFound() => new(LinkResolutionStatus.NotFound, null);

    /// <summary>Creates a <see cref="LinkResolutionStatus.Expired"/> result.</summary>
    /// <returns>A result with no destination.</returns>
    public static LinkResolution Expired() => new(LinkResolutionStatus.Expired, null);

    /// <summary>Creates an <see cref="LinkResolutionStatus.Unavailable"/> result.</summary>
    /// <returns>A result with no destination.</returns>
    public static LinkResolution Unavailable() => new(LinkResolutionStatus.Unavailable, null);
}
