using System.Security.Cryptography;
using Shortener.Links.Api.Application;
using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Infrastructure;

/// <summary>Generates short codes as 7 cryptographically random Base62 characters.</summary>
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <inheritdoc />
    public ShortCode Generate()
    {
        var value = RandomNumberGenerator.GetString(Alphabet, ShortCode.GeneratedLength);

        // The alphabet and length are chosen to always satisfy ShortCode.Create,
        // so a failure here would mean the two have drifted out of sync.
        var result = ShortCode.Create(value);
        return result.Value ?? throw new InvalidOperationException(
            $"Generated code '{value}' failed validation: {result.Error}");
    }
}
