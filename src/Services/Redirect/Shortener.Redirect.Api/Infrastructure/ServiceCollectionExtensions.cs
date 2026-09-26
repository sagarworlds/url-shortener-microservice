using System.Threading.Channels;
using Shortener.Contracts;
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
        builder.AddRabbitMQClient("messaging");

        builder.Services.AddHttpClient<LinksClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://links");
        });

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ILinkCache, RedisLinkCache>();
        builder.Services.AddScoped<ILinkResolver, CacheAsideLinkResolver>();

        // Bounded so a slow or down broker can never build up unbounded
        // memory; ChannelClickPublisher drops and logs a warning once full.
        var clickChannel = Channel.CreateBounded<LinkClicked>(1_000);
        builder.Services.AddSingleton(clickChannel.Writer);
        builder.Services.AddSingleton(clickChannel.Reader);
        builder.Services.AddSingleton<IClickPublisher, ChannelClickPublisher>();
        builder.Services.AddHostedService<ClickEventForwarder>();

        return builder;
    }
}
