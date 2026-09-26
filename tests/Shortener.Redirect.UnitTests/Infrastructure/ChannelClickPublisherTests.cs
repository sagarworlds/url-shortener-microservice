using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shortener.Contracts;
using Shortener.Redirect.Api.Infrastructure;

namespace Shortener.Redirect.UnitTests.Infrastructure;

public class ChannelClickPublisherTests
{
    [Fact]
    public void Publish_ChannelHasSpace_WritesEventWithoutLogging()
    {
        var channel = Channel.CreateBounded<LinkClicked>(1);
        var logger = new RecordingLogger<ChannelClickPublisher>();
        var publisher = new ChannelClickPublisher(channel.Writer, logger);
        var clickEvent = new LinkClicked(Guid.NewGuid(), "aB3xY9z", DateTimeOffset.UtcNow, null);

        publisher.Publish(clickEvent);

        Assert.True(channel.Reader.TryRead(out var written));
        Assert.Equal(clickEvent, written);
        Assert.DoesNotContain(LogLevel.Warning, logger.LoggedLevels);
    }

    [Fact]
    public void Publish_ChannelFull_DropsEventWithoutThrowingAndLogsWarning()
    {
        var channel = Channel.CreateBounded<LinkClicked>(1);
        var logger = new RecordingLogger<ChannelClickPublisher>();
        var publisher = new ChannelClickPublisher(channel.Writer, logger);
        publisher.Publish(new LinkClicked(Guid.NewGuid(), "first", DateTimeOffset.UtcNow, null));

        var dropped = new LinkClicked(Guid.NewGuid(), "second", DateTimeOffset.UtcNow, null);
        var exception = Record.Exception(() => publisher.Publish(dropped));

        Assert.Null(exception);
        Assert.Contains(LogLevel.Warning, logger.LoggedLevels);
        Assert.True(channel.Reader.TryRead(out var remaining));
        Assert.NotEqual(dropped, remaining);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogLevel> LoggedLevels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            LoggedLevels.Add(logLevel);
    }
}
