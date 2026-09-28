using Shortener.Contracts;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Application;

public class DeleteLinkHandlerTests
{
    private const string ValidCode = "aaaaaaa";

    [Fact]
    public async Task HandleAsync_OwnerMatchesCaller_DeletesLink()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var repository = new FakeLinkRepository(link);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.True(result.WasDeleted);
        Assert.True(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_OwnerDiffersFromCaller_ReturnsNotFound()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var repository = new FakeLinkRepository(link);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, "user-2", CancellationToken.None);

        Assert.False(result.WasDeleted);
        Assert.False(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_NoCallerId_DeletesLink()
    {
        var link = CreateLink(ValidCode, ownerId: "user-1");
        var repository = new FakeLinkRepository(link);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, null, CancellationToken.None);

        Assert.True(result.WasDeleted);
        Assert.True(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_NoOwner_DeletesLink()
    {
        var link = CreateLink(ValidCode, ownerId: null);
        var repository = new FakeLinkRepository(link);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.True(result.WasDeleted);
        Assert.True(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_BothNull_DeletesLink()
    {
        var link = CreateLink(ValidCode, ownerId: null);
        var repository = new FakeLinkRepository(link);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, null, CancellationToken.None);

        Assert.True(result.WasDeleted);
        Assert.True(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_CodeNotFound_ReturnsNotFound()
    {
        var repository = new FakeLinkRepository(null);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(ValidCode, "user-1", CancellationToken.None);

        Assert.False(result.WasDeleted);
        Assert.False(repository.DeleteWasCalled);
    }

    [Fact]
    public async Task HandleAsync_InvalidCode_ReturnsNotFound()
    {
        var repository = new FakeLinkRepository(null);
        var handler = new DeleteLinkHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync("!", null, CancellationToken.None);

        Assert.False(result.WasDeleted);
        Assert.False(repository.DeleteWasCalled);
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
        public bool DeleteWasCalled { get; private set; }

        public Task<bool> TryAddAsync(Link link, LinkCreated createdEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Link?> GetByCodeAsync(ShortCode code, CancellationToken cancellationToken) =>
            Task.FromResult(link?.Code.Value == code.Value ? link : null);

        public Task<IReadOnlyList<Link>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteByCodeAsync(ShortCode code, LinkDeleted deletedEvent, CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            return Task.FromResult(link?.Code.Value == code.Value);
        }
    }
}
