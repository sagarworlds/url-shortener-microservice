using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>
/// Constructs an <see cref="AnalyticsDbContext"/> for <c>dotnet ef</c>
/// design-time commands (e.g. <c>migrations add</c>), which run outside
/// Aspire and so don't have the "analyticsdb" connection string Aspire
/// injects at runtime. The connection string here is never used to
/// actually connect: schema generation only needs to know the target is
/// Postgres.
/// </summary>
public sealed class AnalyticsDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    /// <inheritdoc />
    public AnalyticsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql("Host=localhost;Database=analyticsdb;Username=postgres;Password=postgres");

        return new AnalyticsDbContext(optionsBuilder.Options);
    }
}
