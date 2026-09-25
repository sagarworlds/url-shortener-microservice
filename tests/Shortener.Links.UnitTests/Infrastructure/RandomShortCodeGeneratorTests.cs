using Shortener.Links.Api.Domain;
using Shortener.Links.Api.Infrastructure;

namespace Shortener.Links.UnitTests.Infrastructure;

public class RandomShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsCodeOfGeneratedLength()
    {
        var code = _generator.Generate();

        Assert.Equal(ShortCode.GeneratedLength, code.Value.Length);
    }

    [Fact]
    public void Generate_ReturnsOnlyBase62Characters()
    {
        var code = _generator.Generate();

        Assert.All(code.Value, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
    }

    [Fact]
    public void Generate_CalledRepeatedly_ProducesDifferentCodes()
    {
        var codes = Enumerable.Range(0, 20).Select(_ => _generator.Generate().Value).ToHashSet();

        // 62^7 possible codes makes a collision across 20 draws practically
        // impossible; a duplicate here would point to a broken generator.
        Assert.Equal(20, codes.Count);
    }
}
