using Shortener.Contracts;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Application;

public class GetLinkHandlerTests
{
    private const string ValidCode = "aaaaaaa";

    [Fact]
    public async Task HandleAsync_OwnerMatchesCaller_ReturnsFound()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var handler = new GetLinkHandler(new FakeLinkRepository(link));

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(link, result.Link);
    }

    [Fact]
    public async Task HandleAsync_OwnerDiffersFromCaller_ReturnsNotFound()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var handler = new GetLinkHandler(new FakeLinkRepository(link));

        var result = await handler.HandleAsync(ValidCode, "user-2", CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task HandleAsync_NoCallerId_ReturnsFound()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var handler = new GetLinkHandler(new FakeLinkRepository(link));

        var result = await handler.HandleAsync(ValidCode, null, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(link, result.Link);
    }

    [Fact]
    public async Task HandleAsync_NoOwner_ReturnsFound()
    {
        var link = CreateLink(ValidCode, ownerId: null);
        var handler = new GetLinkHandler(new FakeLinkRepository(link));

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(link, result.Link);
    }

    [Fact]
    public async Task HandleAsync_BothNull_ReturnsFound()
    {
        var link = CreateLink(ValidCode, ownerId: null);
        var handler = new GetLinkHandler(new FakeLinkRepository(link));

        var result = await handler.HandleAsync(ValidCode, null, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(link, result.Link);
    }

    [Fact]
    public async Task HandleAsync_CodeNotFound_ReturnsNotFound()
    {
        var handler = new GetLinkHandler(new FakeLinkRepository(null));

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task HandleAsync_InvalidCode_ReturnsNotFound()
    {
        var handler = new GetLinkHandler(new FakeLinkRepository(null));

        var result = await handler.HandleAsync("!", null, CancellationToken.None);

        Assert.False(result.IsFound);
    }

    private static Link CreateLink(string code, string? ownerId) => new(
        Guid.NewGuid(),
        ShortCode.Create(code).Value!,
        DestinationUrl.Create("https://example.com").Value!,
        DateTimeOffset.UtcNow,
        null,
        ownerId);

    private sealed class FakeLinkRepository(Link? link) : ILinkRepository
    {
        public Task<bool> TryAddAsync(Link link, LinkCreated createdEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken) =>
            Task.FromResult(link?.Code.Value == code.Value ? link : null);

        public Task<IReadOnlyList<Link>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteByCodeAsync(ShortCode code, LinkDeleted deletedEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
