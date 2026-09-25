namespace Shortener.Links.Api.Application;

/// <summary>Input for <see cref="CreateLinkHandler"/>.</summary>
/// <param name="Url">The destination URL to shorten.</param>
/// <param name="Alias">
/// An optional caller-chosen short code. When omitted, one is generated.
/// </param>
/// <param name="ExpiresAt">When the link should stop resolving, or <see langword="null"/> for no expiry.</param>
public sealed record CreateLinkRequest(string Url, string? Alias, DateTimeOffset? ExpiresAt);
