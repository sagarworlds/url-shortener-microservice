using Microsoft.EntityFrameworkCore;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>EF Core context for the <c>linksdb</c> database.</summary>
/// <param name="options">Context options, supplied by DI (see <see cref="ServiceCollectionExtensions.AddLinksInfrastructure"/>).</param>
public sealed class LinksDbContext(DbContextOptions<LinksDbContext> options) : DbContext(options)
{
    /// <summary>The links table.</summary>
    public DbSet<Link> Links => Set<Link>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(entity =>
        {
            entity.ToTable("links");
            entity.HasKey(link => link.Id);

            // ShortCode and DestinationUrl are Domain value objects with no EF
            // dependency of their own; the conversion to/from their primitive
            // storage form is an Infrastructure concern, configured here only.
            entity.Property(link => link.Code)
                .HasConversion(code => code.Value, value => ShortCode.Create(value).Value!)
                .HasMaxLength(ShortCode.MaxLength)
                .IsRequired();
            entity.HasIndex(link => link.Code).IsUnique();

            entity.Property(link => link.DestinationUrl)
                .HasConversion(url => url.Value.ToString(), value => DestinationUrl.Create(value).Value!)
                .IsRequired();

            entity.Property(link => link.CreatedAt).IsRequired();
            entity.Property(link => link.ExpiresAt);
        });
    }
}
