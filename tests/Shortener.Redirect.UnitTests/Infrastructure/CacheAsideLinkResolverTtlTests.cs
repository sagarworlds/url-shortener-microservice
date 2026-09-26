using Shortener.Redirect.Api.Infrastructure;

namespace Shortener.Redirect.UnitTests.Infrastructure;

public class CacheAsideLinkResolverTtlTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ComputeFoundTtl_NoExpiry_ReturnsMaxFoundTtl()
    {
        var ttl = CacheAsideLinkResolver.ComputeFoundTtl(null, Now);

        Assert.Equal(CacheAsideLinkResolver.MaxFoundTtl, ttl);
    }

    [Fact]
    public void ComputeFoundTtl_ExpiryFartherThanCap_ReturnsCap()
    {
        var expiresAt = Now.Add(CacheAsideLinkResolver.MaxFoundTtl).AddDays(1);

        var ttl = CacheAsideLinkResolver.ComputeFoundTtl(expiresAt, Now);

        Assert.Equal(CacheAsideLinkResolver.MaxFoundTtl, ttl);
    }

    [Fact]
    public void ComputeFoundTtl_ExpiryWithinCap_ReturnsTimeUntilExpiry()
    {
        var expiresAt = Now.AddHours(1);

        var ttl = CacheAsideLinkResolver.ComputeFoundTtl(expiresAt, Now);

        Assert.Equal(TimeSpan.FromHours(1), ttl);
    }

    [Fact]
    public void ComputeFoundTtl_ExpiryExactlyAtCap_ReturnsCap()
    {
        var expiresAt = Now.Add(CacheAsideLinkResolver.MaxFoundTtl);

        var ttl = CacheAsideLinkResolver.ComputeFoundTtl(expiresAt, Now);

        Assert.Equal(CacheAsideLinkResolver.MaxFoundTtl, ttl);
    }
}
