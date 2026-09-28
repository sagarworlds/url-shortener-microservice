using Shortener.Links.Api.Application;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>Wires the Links API's persistence and application services into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <c>linksdb</c> Postgres context, the repository and
    /// short code generator that back it, and the use-case handlers.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddLinksInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<LinksDbContext>("linksdb");
        builder.AddRabbitMQClient("messaging");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
        builder.Services.AddScoped<ILinkRepository, EfLinkRepository>();
        builder.Services.AddScoped<IOutboxStore, EfOutboxStore>();

        builder.Services.AddScoped<CreateLinkHandler>();
        builder.Services.AddScoped<GetLinkHandler>();
        builder.Services.AddScoped<DeleteLinkHandler>();
        builder.Services.AddScoped<BackfillOutboxHandler>();

        builder.Services.AddHostedService<OutboxDispatcher>();

        return builder;
    }
}
