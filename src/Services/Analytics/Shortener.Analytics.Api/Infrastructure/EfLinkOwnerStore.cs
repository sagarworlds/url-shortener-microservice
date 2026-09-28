using Microsoft.EntityFrameworkCore;
using Shortener.Analytics.Api.Application;
using Shortener.Analytics.Api.Domain;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>EF Core / Postgres implementation of <see cref="ILinkOwnerStore"/> and <see cref="ILinkOwnerQuery"/>.</summary>
public sealed class EfLinkOwnerStore(AnalyticsDbContext dbContext) : ILinkOwnerStore, ILinkOwnerQuery
{
    /// <inheritdoc />
    public async Task SetOwnerAsync(string code, string ownerId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.LinkOwners.FindAsync([code], cancellationToken);
        if (existing is null)
        {
            dbContext.LinkOwners.Add(new LinkOwner(code, ownerId));
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(new LinkOwner(code, ownerId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveOwnerAsync(string code, CancellationToken cancellationToken)
    {
        await dbContext.LinkOwners.Where(owner => owner.Code == code).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetOwnerAsync(string code, CancellationToken cancellationToken)
    {
        var owner = await dbContext.LinkOwners.FindAsync([code], cancellationToken);
        return owner?.OwnerId;
    }
}
