using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shortener.Redirect.Api.Application;
using Shortener.Redirect.Api.Infrastructure;

namespace Shortener.Redirect.UnitTests.Infrastructure;

public class CacheAsideLinkResolverTests
{
    [Theory]
    [MemberData(nameof(UnavailableExceptions))]
    public async Task ResolveAsync_LinksThrowsUnavailableException_ReturnsUnavailableAndCachesNothing(Exception exception)
    {
        var cache = new FakeLinkCache();
        var linksClient = new LinksClient(new HttpClient(new ThrowingHttpMessageHandler(exception))
        {
            BaseAddress = new Uri("http://links.test"),
        });
        var resolver = new CacheAsideLinkResolver(
            cache,
            linksClient,
            TimeProvider.System,
            NullLogger<CacheAsideLinkResolver>.Instance);

        var result = await resolver.ResolveAsync("aB3xY9z", CancellationToken.None);

        Assert.Equal(LinkResolutionStatus.Unavailable, result.Status);
        Assert.False(cache.SetWasCalled);
    }

    public static TheoryData<Exception> UnavailableExceptions() =>
    [
        new HttpRequestException("connection refused"),
        new BrokenCircuitException(),
        new TimeoutRejectedException(),
    ];

    private sealed class FakeLinkCache : ILinkCache
    {
        public bool SetWasCalled { get; private set; }

        public Task<CachedLinkEntry?> GetAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<CachedLinkEntry?>(null);

        public Task SetAsync(string code, CachedLinkEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            SetWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }
}
