namespace Shortener.Links.Api.Domain;

/// <summary>
/// The URL a short link redirects to. Only absolute <c>http</c> and
/// <c>https</c> URLs are accepted, so a short link can never be created for
/// a relative path or a non-web scheme such as <c>javascript:</c>.
/// </summary>
public sealed class DestinationUrl
{
    private DestinationUrl(Uri value) => Value = value;

    /// <summary>The validated absolute URL.</summary>
    public Uri Value { get; }

    /// <summary>
    /// Validates <paramref name="value"/> and, if valid, wraps it as a <see cref="DestinationUrl"/>.
    /// </summary>
    /// <param name="value">The candidate destination URL.</param>
    /// <returns>A successful result wrapping the URL, or a failed result with the rejection reason.</returns>
    public static ValidationResult<DestinationUrl> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult<DestinationUrl>.Failure("Destination URL cannot be empty.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidationResult<DestinationUrl>.Failure(
                "Destination URL must be an absolute http or https URL.");
        }

        return ValidationResult<DestinationUrl>.Success(new DestinationUrl(uri));
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
