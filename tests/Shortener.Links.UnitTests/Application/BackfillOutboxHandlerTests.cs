using Shortener.Contracts;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Application;

public class BackfillOutboxHandlerTests
{
    [Fact]
    public async Task HandleAsync_EnqueuesOneEventPerLinkAndSavesOnce()
    {
        var links = new[]
        {
            CreateLink("aaaaaaa", "https://example.com/a"),
            CreateLink("bbbbbbb", "https://example.com/b"),
        };
        var repository = new FakeLinkRepository(links);
        var outboxStore = new FakeOutboxStore();
        var handler = new BackfillOutboxHandler(repository, outboxStore);

        var count = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(2, outboxStore.EnqueuedCreatedEvents.Count);
        Assert.Equal(1, outboxStore.SaveChangesCallCount);
        Assert.Contains(outboxStore.EnqueuedCreatedEvents, e => e.Code == "aaaaaaa");
        Assert.Contains(outboxStore.EnqueuedCreatedEvents, e => e.Code == "bbbbbbb");
    }

    [Fact]
    public async Task HandleAsync_NoLinks_SavesOnceWithNothingEnqueued()
    {
        var repository = new FakeLinkRepository([]);
        var outboxStore = new FakeOutboxStore();
        var handler = new BackfillOutboxHandler(repository, outboxStore);

        var count = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(outboxStore.EnqueuedCreatedEvents);
        Assert.Equal(1, outboxStore.SaveChangesCallCount);
    }

    private static Link CreateLink(string code, string url) => new(
        Guid.NewGuid(),
        ShortCode.Create(code).Value!,
        DestinationUrl.Create(url).Value!,
        DateTimeOffset.UtcNow,
        null);

    private sealed class FakeLinkRepository(IReadOnlyList<Link> links) : ILinkRepository
    {
        public Task<bool> TryAddAsync(Link link, LinkCreated createdEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Link>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult(links);

        public Task<bool> DeleteByCodeAsync(ShortCode code, LinkDeleted deletedEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOutboxStore : IOutboxStore
    {
        public List<LinkCreated> EnqueuedCreatedEvents { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public void Enqueue(LinkCreated createdEvent) => EnqueuedCreatedEvents.Add(createdEvent);

        public void Enqueue(LinkDeleted deletedEvent) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
