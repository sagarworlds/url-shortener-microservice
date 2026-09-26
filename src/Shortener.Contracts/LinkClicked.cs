namespace Shortener.Contracts;

/// <summary>
/// Raised by Redirect when a short code resolves to a live destination and a
/// redirect is issued. Consumed by Analytics to build click stats.
/// </summary>
/// <param name="EventId">Unique id for this occurrence, used by consumers to de-duplicate at-least-once delivery.</param>
/// <param name="Code">The short code that was redirected.</param>
/// <param name="OccurredAt">When the redirect happened.</param>
/// <param name="Referrer">The client's <c>Referer</c> request header, if it sent one.</param>
public sealed record LinkClicked(Guid EventId, string Code, DateTimeOffset OccurredAt, string? Referrer);
