using Shortener.Links.Api.Domain;

namespace Shortener.Links.UnitTests.Domain;

public class ShortCodeTests
{
    [Theory]
    [InlineData("aB3xY9z")]
    [InlineData("abc")]
    [InlineData("customAliasLikeName1234567890AB")]
    public void Create_ValidBase62Value_ReturnsValid(string value)
    {
        var result = ShortCode.Create(value);

        Assert.True(result.IsValid);
        Assert.Equal(value, result.Value!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespace_ReturnsInvalid(string? value)
    {
        var result = ShortCode.Create(value);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Create_ShorterThanMinLength_ReturnsInvalid()
    {
        var tooShort = new string('a', ShortCode.MinLength - 1);

        var result = ShortCode.Create(tooShort);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_LongerThanMaxLength_ReturnsInvalid()
    {
        var tooLong = new string('a', ShortCode.MaxLength + 1);

        var result = ShortCode.Create(tooLong);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has_underscore")]
    [InlineData("has/slash")]
    [InlineData("emoji😀code")]
    public void Create_DisallowedCharacter_ReturnsInvalid(string value)
    {
        var result = ShortCode.Create(value);

        Assert.False(result.IsValid);
    }
}
