using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Domain;

public class LinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsExpired_NoExpiry_ReturnsFalse()
    {
        var link = CreateLink(expiresAt: null);

        Assert.False(link.IsExpired(Now));
    }

    [Fact]
    public void IsExpired_ExpiryInFuture_ReturnsFalse()
    {
        var link = CreateLink(expiresAt: Now.AddMinutes(1));

        Assert.False(link.IsExpired(Now));
    }

    [Fact]
    public void IsExpired_ExpiryInPast_ReturnsTrue()
    {
        var link = CreateLink(expiresAt: Now.AddMinutes(-1));

        Assert.True(link.IsExpired(Now));
    }

    [Fact]
    public void IsExpired_ExpiryEqualsAsOf_ReturnsTrue()
    {
        var link = CreateLink(expiresAt: Now);

        Assert.True(link.IsExpired(Now));
    }

    private static Link CreateLink(DateTimeOffset? expiresAt) => new(
        Guid.NewGuid(),
        ShortCode.Create("aB3xY9z").Value!,
        DestinationUrl.Create("https://example.com").Value!,
        Now,
        expiresAt);
}
