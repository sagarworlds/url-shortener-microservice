using Shortener.Analytics.Api.Application;

namespace Shortener.Analytics.UnitTests.Application;

public class GetClickStatsHandlerTests
{
    private static readonly ClickStats SomeStats = new("aaaaaaa", 5, [new DailyClicks(new DateOnly(2026, 1, 1), 5)]);

    [Fact]
    public async Task HandleAsync_OwnerMatchesCaller_ReturnsFound()
    {
        var handler = new GetClickStatsHandler(
            new FakeClickStatsQuery(SomeStats),
            new FakeLinkOwnerQuery("user-1"));

        var result = await handler.HandleAsync("aaaaaaa", "user-1", CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(SomeStats, result.Stats);
    }

    [Fact]
    public async Task HandleAsync_OwnerDiffersFromCaller_ReturnsNotFound()
    {
        var handler = new GetClickStatsHandler(
            new FakeClickStatsQuery(SomeStats),
            new FakeLinkOwnerQuery("user-1"));

        var result = await handler.HandleAsync("aaaaaaa", "user-2", CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task HandleAsync_NoCallerId_ReturnsFound()
    {
        var handler = new GetClickStatsHandler(
            new FakeClickStatsQuery(SomeStats),
            new FakeLinkOwnerQuery("user-1"));

        var result = await handler.HandleAsync("aaaaaaa", null, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(SomeStats, result.Stats);
    }

    [Fact]
    public async Task HandleAsync_NoOwner_ReturnsFound()
    {
        var handler = new GetClickStatsHandler(
            new FakeClickStatsQuery(SomeStats),
            new FakeLinkOwnerQuery(null));

        var result = await handler.HandleAsync("aaaaaaa", "user-1", CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(SomeStats, result.Stats);
    }

    [Fact]
    public async Task HandleAsync_BothNull_ReturnsFound()
    {
        var handler = new GetClickStatsHandler(
            new FakeClickStatsQuery(SomeStats),
            new FakeLinkOwnerQuery(null));

        var result = await handler.HandleAsync("aaaaaaa", null, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(SomeStats, result.Stats);
    }

    private sealed class FakeClickStatsQuery(ClickStats stats) : IClickStatsQuery
    {
        public Task<ClickStats> GetStatsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(stats);
    }

    private sealed class FakeLinkOwnerQuery(string? ownerId) : ILinkOwnerQuery
    {
        public Task<string?> GetOwnerAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(ownerId);
    }
}
