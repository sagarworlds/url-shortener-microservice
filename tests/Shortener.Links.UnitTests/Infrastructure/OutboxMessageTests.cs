using System.Text.Json;
using Shortener.Contracts;
using Shortener.Links.Api.Infrastructure;

namespace Shortener.Links.UnitTests.Infrastructure;

public class OutboxMessageTests
{
    [Fact]
    public void For_LinkCreated_WrapsEventCorrectly()
    {
        var createdEvent = new LinkCreated(Guid.NewGuid(), "aB3xY9z", "https://example.com", null, DateTimeOffset.UtcNow);

        var message = OutboxMessage.For(createdEvent);

        Assert.Equal(createdEvent.EventId, message.Id);
        Assert.Equal(nameof(LinkCreated), message.Type);
        Assert.Equal(createdEvent.OccurredAt, message.OccurredAt);
        Assert.Null(message.DispatchedAt);
        Assert.Equal(createdEvent, JsonSerializer.Deserialize<LinkCreated>(message.Payload));
    }

    [Fact]
    public void For_LinkDeleted_WrapsEventCorrectly()
    {
        var deletedEvent = new LinkDeleted(Guid.NewGuid(), "aB3xY9z", DateTimeOffset.UtcNow);

        var message = OutboxMessage.For(deletedEvent);

        Assert.Equal(deletedEvent.EventId, message.Id);
        Assert.Equal(nameof(LinkDeleted), message.Type);
        Assert.Equal(deletedEvent.OccurredAt, message.OccurredAt);
        Assert.Null(message.DispatchedAt);
        Assert.Equal(deletedEvent, JsonSerializer.Deserialize<LinkDeleted>(message.Payload));
    }

    [Fact]
    public void MarkDispatched_SetsDispatchedAt()
    {
        var message = OutboxMessage.For(new LinkDeleted(Guid.NewGuid(), "aB3xY9z", DateTimeOffset.UtcNow));
        var dispatchedAt = DateTimeOffset.UtcNow;

        message.MarkDispatched(dispatchedAt);

        Assert.Equal(dispatchedAt, message.DispatchedAt);
    }
}
