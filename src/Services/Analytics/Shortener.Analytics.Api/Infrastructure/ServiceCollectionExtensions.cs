using Shortener.Analytics.Api.Application;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>Wires the Analytics API's persistence and application services into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <c>analyticsdb</c> Postgres context, the store that
    /// backs it, and the use-case handlers.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddAnalyticsInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AnalyticsDbContext>("analyticsdb");

        builder.Services.AddScoped<EfClickStore>();
        builder.Services.AddScoped<IClickRecorder>(sp => sp.GetRequiredService<EfClickStore>());
        builder.Services.AddScoped<IClickStatsQuery>(sp => sp.GetRequiredService<EfClickStore>());

        builder.Services.AddScoped<RecordClickHandler>();
        builder.Services.AddScoped<GetClickStatsHandler>();

        return builder;
    }
}
