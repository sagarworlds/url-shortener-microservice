using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shortener.Contracts;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>EF Core / Postgres implementation of <see cref="ILinkRepository"/>.</summary>
public sealed class EfLinkRepository(LinksDbContext dbContext) : ILinkRepository
{
    /// <inheritdoc />
    public async Task<bool> TryAddAsync(Link link, LinkCreated createdEvent, CancellationToken cancellationToken)
    {
        dbContext.Add(link);
        var outboxMessage = OutboxMessage.For(createdEvent);
        dbContext.Add(outboxMessage);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Detach both so the failed attempt doesn't poison the next
            // SaveChanges call on this context (retry with a new code and a
            // new event reuses the scope).
            dbContext.Entry(link).State = EntityState.Detached;
            dbContext.Entry(outboxMessage).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken) =>
        dbContext.Links.SingleOrDefaultAsync(link => link.Code == code, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Link>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Links.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteByCodeAsync(ShortCode code, LinkDeleted deletedEvent, CancellationToken cancellationToken)
    {
        var link = await dbContext.Links.SingleOrDefaultAsync(l => l.Code == code, cancellationToken);
        if (link is null)
        {
            return false;
        }

        dbContext.Remove(link);
        dbContext.Add(OutboxMessage.For(deletedEvent));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
