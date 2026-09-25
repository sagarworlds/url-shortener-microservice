using Shortener.Links.Api.Domain;

namespace Shortener.Links.Api.Application;

/// <summary>Generates short codes for links that don't have a caller-supplied alias.</summary>
public interface IShortCodeGenerator
{
    /// <summary>Generates a new, randomly chosen short code.</summary>
    /// <returns>A valid <see cref="ShortCode"/>. Uniqueness is enforced by the repository, not the generator.</returns>
    ShortCode Generate();
}
