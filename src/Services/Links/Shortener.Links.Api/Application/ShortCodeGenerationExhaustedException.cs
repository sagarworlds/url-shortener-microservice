namespace Shortener.Links.Api.Application;

/// <summary>
/// Thrown when <see cref="CreateLinkHandler"/> could not find an unused
/// generated short code within its retry budget. With 7 Base62 characters
/// (~3.5 * 10^12 possible codes) this signals something is systemically
/// wrong rather than ordinary bad luck, so it is treated as an unexpected
/// failure and handled by the global exception handler.
/// </summary>
public sealed class ShortCodeGenerationExhaustedException()
    : Exception("Could not generate a unique short code after 3 attempts.");
