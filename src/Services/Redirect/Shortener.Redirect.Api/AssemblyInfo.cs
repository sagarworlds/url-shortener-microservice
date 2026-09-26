using System.Runtime.CompilerServices;

// Exposes internal-only types (ILinkCache, CachedLinkEntry, and
// CacheAsideLinkResolver's internal test seams) to the unit test project,
// without making them part of the service's public API surface.
[assembly: InternalsVisibleTo("Shortener.Redirect.UnitTests")]
