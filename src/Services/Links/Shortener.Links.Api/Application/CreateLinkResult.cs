using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>The possible outcomes of <see cref="CreateLinkHandler"/>.</summary>
public enum CreateLinkStatus
{
    /// <summary>The link was created.</summary>
    Created,

    /// <summary>The URL or alias failed validation.</summary>
    Invalid,

    /// <summary>The caller-supplied alias is already in use.</summary>
    AliasTaken,
}

/// <summary>Result of <see cref="CreateLinkHandler"/>.</summary>
public sealed class CreateLinkResult
{
    private CreateLinkResult(CreateLinkStatus status, Link? link, string? error)
    {
        Status = status;
        Link = link;
        Error = error;
    }

    /// <summary>Which of the possible outcomes occurred.</summary>
    public CreateLinkStatus Status { get; }

    /// <summary>The created link when <see cref="Status"/> is <see cref="CreateLinkStatus.Created"/>; otherwise <see langword="null"/>.</summary>
    public Link? Link { get; }

    /// <summary>The validation error when <see cref="Status"/> is <see cref="CreateLinkStatus.Invalid"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Creates a <see cref="CreateLinkStatus.Created"/> result.</summary>
    /// <param name="link">The link that was created.</param>
    /// <returns>A result wrapping <paramref name="link"/>.</returns>
    public static CreateLinkResult Created(Link link) => new(CreateLinkStatus.Created, link, null);

    /// <summary>Creates an <see cref="CreateLinkStatus.Invalid"/> result.</summary>
    /// <param name="error">Why the input was rejected.</param>
    /// <returns>A result carrying the rejection reason.</returns>
    public static CreateLinkResult Invalid(string error) => new(CreateLinkStatus.Invalid, null, error);

    /// <summary>Creates an <see cref="CreateLinkStatus.AliasTaken"/> result.</summary>
    /// <returns>A result with no link or error payload.</returns>
    public static CreateLinkResult AliasTaken() => new(CreateLinkStatus.AliasTaken, null, null);
}
