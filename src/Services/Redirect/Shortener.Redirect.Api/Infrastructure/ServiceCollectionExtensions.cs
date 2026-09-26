using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>Wires the Redirect API's cache, HTTP client and resolver into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Redis client, the typed <see cref="LinksClient"/> (routed
    /// through Aspire service discovery to <c>links</c>), and the
    /// <see cref="CacheAsideLinkResolver"/> that ties them together.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddRedirectInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient("redis");

        builder.Services.AddHttpClient<LinksClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://links");
        });

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ILinkResolver, CacheAsideLinkResolver>();

        return builder;
    }
}
