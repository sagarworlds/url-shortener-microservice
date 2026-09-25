namespace Shortener.Links.Api.Domain;

/// <summary>
/// The code segment of a short link (e.g. the <c>aB3xY9z</c> in
/// <c>https://sho.rt/aB3xY9z</c>). Generated codes are always 7 Base62
/// characters; caller-supplied aliases are validated against the same
/// alphabet but may be a different length, within bounds that keep them
/// safe to use as a URL path segment.
/// </summary>
public sealed class ShortCode
{
    /// <summary>The shortest allowed code, generated or alias.</summary>
    public const int MinLength = 3;

    /// <summary>The longest allowed code, generated or alias.</summary>
    public const int MaxLength = 32;

    /// <summary>The length of a generated (non-alias) code.</summary>
    public const int GeneratedLength = 7;

    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private ShortCode(string value) => Value = value;

    /// <summary>The code's string form, e.g. <c>aB3xY9z</c>.</summary>
    public string Value { get; }

    /// <summary>
    /// Validates <paramref name="value"/> and, if valid, wraps it as a <see cref="ShortCode"/>.
    /// </summary>
    /// <param name="value">The candidate code, generated or caller-supplied.</param>
    /// <returns>A successful result wrapping the code, or a failed result with the rejection reason.</returns>
    public static ValidationResult<ShortCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult<ShortCode>.Failure("Short code cannot be empty.");
        }

        if (value.Length is < MinLength or > MaxLength)
        {
            return ValidationResult<ShortCode>.Failure(
                $"Short code must be between {MinLength} and {MaxLength} characters.");
        }

        foreach (var c in value)
        {
            if (!Alphabet.Contains(c))
            {
                return ValidationResult<ShortCode>.Failure(
                    "Short code may only contain letters (A-Z, a-z) and digits (0-9).");
            }
        }

        return ValidationResult<ShortCode>.Success(new ShortCode(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
