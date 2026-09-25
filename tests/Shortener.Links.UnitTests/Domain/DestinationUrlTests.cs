using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Domain;

public class DestinationUrlTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/some/path?query=1")]
    public void Create_AbsoluteHttpOrHttpsUrl_ReturnsValid(string value)
    {
        var result = DestinationUrl.Create(value);

        Assert.True(result.IsValid);
        Assert.Equal(value, result.Value!.Value.ToString().TrimEnd('/'));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespace_ReturnsInvalid(string? value)
    {
        var result = DestinationUrl.Create(value);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    public void Create_RelativeOrNonHttpScheme_ReturnsInvalid(string value)
    {
        var result = DestinationUrl.Create(value);

        Assert.False(result.IsValid);
    }
}
