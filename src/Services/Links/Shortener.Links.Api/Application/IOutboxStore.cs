using Shortener.Contracts;

namespace Shortener.Links.Api.Application;

/// <summary>
/// Stages events for later dispatch. Used by the backfill path, which has
/// no collision/retry concerns and so can stage many events and save once —
/// unlike <see cref="ILinkRepository"/>'s writes, which must stage and save
/// their event in the exact same call as the link change.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Stages a <see cref="LinkCreated"/> event.</summary>
    /// <param name="createdEvent">The event to stage.</param>
    void Enqueue(LinkCreated createdEvent);

    /// <summary>Stages a <see cref="LinkDeleted"/> event.</summary>
    /// <param name="deletedEvent">The event to stage.</param>
    void Enqueue(LinkDeleted deletedEvent);

    /// <summary>Persists every event staged so far.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
