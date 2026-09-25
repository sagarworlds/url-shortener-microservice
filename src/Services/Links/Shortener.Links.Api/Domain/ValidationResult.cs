namespace Shortener.Links.Api.Domain;

/// <summary>
/// Outcome of a value object factory method: either a valid instance or a
/// human-readable reason the input was rejected.
/// </summary>
/// <typeparam name="T">The type being validated and constructed.</typeparam>
public readonly struct ValidationResult<T>
{
    private ValidationResult(bool isValid, T? value, string? error)
    {
        IsValid = isValid;
        Value = value;
        Error = error;
    }

    /// <summary>Whether the input passed validation.</summary>
    public bool IsValid { get; }

    /// <summary>The constructed instance when <see cref="IsValid"/> is <see langword="true"/>; otherwise <see langword="default"/>.</summary>
    public T? Value { get; }

    /// <summary>A human-readable rejection reason when <see cref="IsValid"/> is <see langword="false"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    /// <param name="value">The validated instance.</param>
    /// <returns>A successful <see cref="ValidationResult{T}"/>.</returns>
    public static ValidationResult<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result carrying a rejection reason.</summary>
    /// <param name="error">Why the input was rejected.</param>
    /// <returns>A failed <see cref="ValidationResult{T}"/>.</returns>
    public static ValidationResult<T> Failure(string error) => new(false, default, error);
}
