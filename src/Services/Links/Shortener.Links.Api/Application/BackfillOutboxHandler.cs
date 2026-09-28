using Shortener.Contracts;

namespace Shortener.Links.Api.Application;

/// <summary>Handles the backfill-the-outbox use case: republish every existing link.</summary>
public sealed class BackfillOutboxHandler(ILinkRepository repository, IOutboxStore outboxStore)
{
    /// <summary>Stages a <see cref="LinkCreated"/> event for every link currently stored.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>How many links were backfilled.</returns>
    public async Task<int> HandleAsync(CancellationToken cancellationToken)
    {
        var links = await repository.GetAllAsync(cancellationToken);

        foreach (var link in links)
        {
            var createdEvent = new LinkCreated(
                Guid.NewGuid(),
                link.Code.Value,
                link.DestinationUrl.Value.ToString(),
                link.ExpiresAt,
                link.CreatedAt,
                link.OwnerId);

            outboxStore.Enqueue(createdEvent);
        }

        await outboxStore.SaveChangesAsync(cancellationToken);
        return links.Count;
    }
}
