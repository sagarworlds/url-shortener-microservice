namespace Shortener.Analytics.Api.Domain;

/// <summary>
/// Maps a short code to the identity of the user who created it.
/// Built from <c>LinkCreated</c> events to enforce ownership checks
/// without crossing the database-per-service boundary.
/// </summary>
/// <param name="Code">The short code (primary key).</param>
/// <param name="OwnerId">The identity of the user who created this link.</param>
public sealed record LinkOwner(string Code, string OwnerId);
