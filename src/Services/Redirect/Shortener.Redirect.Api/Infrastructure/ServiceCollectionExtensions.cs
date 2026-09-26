using System.Threading.Channels;
using Shortener.Contracts;
using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>Wires the Redirect API's cache, HTTP client and resolver into DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Redis client, the <see cref="ReadModelLinkResolver"/>
    /// that reads its read model, the <see cref="LinkEventConsumer"/> that
    /// feeds it from Links' outbox, and click-event publishing. M1's
    /// <c>CacheAsideLinkResolver</c>/<c>ILinkCache</c>/<c>LinksClient</c>
    /// stay in the codebase, unregistered — swapping <c>ILinkResolver</c>'s
    /// implementation back is a one-line change if ever needed.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddRedirectInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient("redis");
        builder.AddRabbitMQClient("messaging");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IReadModelStore, RedisReadModelStore>();
        builder.Services.AddScoped<ILinkResolver, ReadModelLinkResolver>();
        builder.Services.AddHostedService<LinkEventConsumer>();

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
