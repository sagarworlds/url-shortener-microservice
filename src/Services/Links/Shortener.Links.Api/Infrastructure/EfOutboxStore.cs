using Shortener.Contracts;
using Shortener.Links.Api.Application;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>EF Core implementation of <see cref="IOutboxStore"/>.</summary>
public sealed class EfOutboxStore(LinksDbContext dbContext) : IOutboxStore
{
    /// <inheritdoc />
    public void Enqueue(LinkCreated createdEvent) => dbContext.Add(OutboxMessage.For(createdEvent));

    /// <inheritdoc />
    public void Enqueue(LinkDeleted deletedEvent) => dbContext.Add(OutboxMessage.For(deletedEvent));

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
