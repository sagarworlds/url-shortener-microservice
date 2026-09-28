using Microsoft.EntityFrameworkCore;
using Shortener.Analytics.Api.Domain;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>EF Core context for the <c>analyticsdb</c> database.</summary>
/// <param name="options">Context options, supplied by DI (see <see cref="ServiceCollectionExtensions.AddAnalyticsInfrastructure"/>).</param>
public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    /// <summary>Per-code, per-day click counts.</summary>
    public DbSet<DailyClickCount> DailyClickCounts => Set<DailyClickCount>();

    /// <summary>Event ids already applied, for idempotent redelivery.</summary>
    internal DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    /// <summary>Link-to-owner mapping projected from <c>LinkCreated</c> events.</summary>
    public DbSet<LinkOwner> LinkOwners => Set<LinkOwner>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyClickCount>(entity =>
        {
            entity.ToTable("daily_click_counts");
            entity.HasKey(count => new { count.Code, count.Date });
            entity.Property(count => count.Code).IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("processed_events");
            entity.HasKey(processed => processed.EventId);
            entity.Property(processed => processed.ProcessedAt).IsRequired();
        });

        modelBuilder.Entity<LinkOwner>(entity =>
        {
            entity.ToTable("link_owners");
            entity.HasKey(owner => owner.Code);
            entity.Property(owner => owner.Code).IsRequired();
            entity.Property(owner => owner.OwnerId).IsRequired();
        });
    }
}
