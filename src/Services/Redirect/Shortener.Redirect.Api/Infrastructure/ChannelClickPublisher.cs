using System.Threading.Channels;
using Shortener.Contracts;
using Shortener.Redirect.Api.Application;

namespace Shortener.Redirect.Api.Infrastructure;

/// <summary>
/// Writes click events to a bounded in-memory channel for
/// <see cref="ClickEventForwarder"/> to drain and publish to RabbitMQ. Never
/// blocks the redirect hot path: a full channel just drops the event.
/// </summary>
/// <param name="writer">The write side of the shared channel.</param>
/// <param name="logger">Logs dropped events when the channel is full.</param>
public sealed class ChannelClickPublisher(ChannelWriter<LinkClicked> writer, ILogger<ChannelClickPublisher> logger) : IClickPublisher
{
    /// <inheritdoc />
    public void Publish(LinkClicked clickEvent)
    {
        if (!writer.TryWrite(clickEvent))
        {
            logger.LogWarning("Click event channel is full; dropping click for code {Code}", clickEvent.Code);
        }
    }
}
