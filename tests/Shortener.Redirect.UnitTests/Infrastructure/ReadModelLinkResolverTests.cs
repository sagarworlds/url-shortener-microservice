using Microsoft.Extensions.Logging.Abstractions;
using Shortener.Redirect.Api.Application;
using Shortener.Redirect.Api.Infrastructure;
using StackExchange.Redis;

namespace Shortener.Redirect.UnitTests.Infrastructure;

public class ReadModelLinkResolverTests
{
    [Fact]
    public async Task ResolveAsync_StoreHasValue_ReturnsFound()
    {
        var store = new FakeReadModelStore { ValueToReturn = "https://example.com/target" };
        var resolver = new ReadModelLinkResolver(store, NullLogger<ReadModelLinkResolver>.Instance);

        var result = await resolver.ResolveAsync("aB3xY9z", CancellationToken.None);

        Assert.Equal(LinkResolutionStatus.Found, result.Status);
        Assert.Equal(new Uri("https://example.com/target"), result.Destination);
    }

    [Fact]
    public async Task ResolveAsync_StoreHasNoValue_ReturnsNotFound()
    {
        var store = new FakeReadModelStore { ValueToReturn = null };
        var resolver = new ReadModelLinkResolver(store, NullLogger<ReadModelLinkResolver>.Instance);

        var result = await resolver.ResolveAsync("aB3xY9z", CancellationToken.None);

        Assert.Equal(LinkResolutionStatus.NotFound, result.Status);
    }

    [Theory]
    [MemberData(nameof(UnavailableExceptions))]
    public async Task ResolveAsync_StoreThrowsRedisException_ReturnsUnavailable(Exception exception)
    {
        var store = new FakeReadModelStore { ExceptionToThrow = exception };
        var resolver = new ReadModelLinkResolver(store, NullLogger<ReadModelLinkResolver>.Instance);

        var result = await resolver.ResolveAsync("aB3xY9z", CancellationToken.None);

        Assert.Equal(LinkResolutionStatus.Unavailable, result.Status);
    }

    public static TheoryData<Exception> UnavailableExceptions() =>
    [
        new RedisConnectionException(ConnectionFailureType.UnableToConnect, "connection refused"),
        new RedisTimeoutException("timed out", CommandStatus.Unknown),
    ];

    private sealed class FakeReadModelStore : IReadModelStore
    {
        public string? ValueToReturn { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task SetAsync(string code, string destinationUrl, TimeSpan? ttl, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> GetAsync(string code, CancellationToken cancellationToken)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ValueToReturn);
        }

        public Task DeleteAsync(string code, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
