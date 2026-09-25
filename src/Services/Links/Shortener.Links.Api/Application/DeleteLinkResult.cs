namespace Shortener.Links.Api.Application;

/// <summary>Result of <see cref="DeleteLinkHandler"/>.</summary>
public sealed class DeleteLinkResult
{
    private DeleteLinkResult(bool wasDeleted) => WasDeleted = wasDeleted;

    /// <summary>Whether a link was found and deleted.</summary>
    public bool WasDeleted { get; }

    /// <summary>Creates a result for a successful deletion.</summary>
    /// <returns>A result with <see cref="WasDeleted"/> set to <see langword="true"/>.</returns>
    public static DeleteLinkResult Deleted() => new(true);

    /// <summary>Creates a result for no matching link.</summary>
    /// <returns>A result with <see cref="WasDeleted"/> set to <see langword="false"/>.</returns>
    public static DeleteLinkResult NotFound() => new(false);
}
