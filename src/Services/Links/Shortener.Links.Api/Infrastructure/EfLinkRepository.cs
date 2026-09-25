using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>EF Core / Postgres implementation of <see cref="ILinkRepository"/>.</summary>
public sealed class EfLinkRepository(LinksDbContext dbContext) : ILinkRepository
{
    /// <inheritdoc />
    public async Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Add(link);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Detach so the failed insert doesn't poison the next SaveChanges
            // call on this context (retry with a new code reuses the scope).
            dbContext.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken) =>
        dbContext.Links.SingleOrDefaultAsync(link => link.Code == code, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteByCodeAsync(ShortCode code, CancellationToken cancellationToken)
    {
        var link = await dbContext.Links.SingleOrDefaultAsync(l => l.Code == code, cancellationToken);
        if (link is null)
        {
            return false;
        }

        dbContext.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
