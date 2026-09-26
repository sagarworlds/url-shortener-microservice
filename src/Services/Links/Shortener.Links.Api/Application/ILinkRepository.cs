using Shortener.Contracts;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Persists and retrieves <see cref="Link"/>s by their short code.</summary>
public interface ILinkRepository
{
    /// <summary>
    /// Attempts to add a new link, staging <paramref name="createdEvent"/> as
    /// an outbox row in the exact same transaction. A collision on
    /// <see cref="Link.Code"/> is an expected outcome (the caller decides
    /// whether to retry with a new generated code or report a conflict for
    /// an alias), so it is signalled through the return value rather than an
    /// exception — and rolls back both the link and the staged event, so a
    /// failed attempt never leaks into the next one.
    /// </summary>
    /// <param name="link">The link to add.</param>
    /// <param name="createdEvent">The event to stage alongside it.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if the link was added; <see langword="false"/> if its code already exists.</returns>
    Task<bool> TryAddAsync(Link link, LinkCreated createdEvent, CancellationToken cancellationToken);

    /// <summary>Looks up a link by its short code.</summary>
    /// <param name="code">The short code to look up.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching <see cref="Link"/>, or <see langword="null"/> if no link has that code.</returns>
    Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken);

    /// <summary>Gets every link, for backfilling the outbox.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Every link currently stored.</returns>
    Task<IReadOnlyList<Link>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the link with the given short code, if one exists, staging
    /// <paramref name="deletedEvent"/> as an outbox row in the same
    /// transaction as the deletion.
    /// </summary>
    /// <param name="code">The short code to delete.</param>
    /// <param name="deletedEvent">The event to stage if a link is deleted.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if a link was deleted; <see langword="false"/> if no link had that code.</returns>
    Task<bool> DeleteByCodeAsync(ShortCode code, LinkDeleted deletedEvent, CancellationToken cancellationToken);
}
