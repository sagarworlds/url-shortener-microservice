using System.Runtime.CompilerServices;

// Exposes internal-only types (OutboxMessage) to the unit test project,
// without making them part of the service's public API surface.
[assembly: InternalsVisibleTo("Shortener.Links.UnitTests")]
