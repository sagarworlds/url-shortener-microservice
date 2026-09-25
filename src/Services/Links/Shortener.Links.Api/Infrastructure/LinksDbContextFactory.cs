using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>
/// Constructs a <see cref="LinksDbContext"/> for <c>dotnet ef</c> design-time
/// commands (e.g. <c>migrations add</c>), which run outside Aspire and so
/// don't have the "linksdb" connection string Aspire injects at runtime.
/// The connection string here is never used to actually connect: schema
/// generation only needs to know the target is Postgres.
/// </summary>
public sealed class LinksDbContextFactory : IDesignTimeDbContextFactory<LinksDbContext>
{
    /// <inheritdoc />
    public LinksDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LinksDbContext>()
            .UseNpgsql("Host=localhost;Database=linksdb;Username=postgres;Password=postgres");

        return new LinksDbContext(optionsBuilder.Options);
    }
}
