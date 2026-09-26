using Shortener.Contracts;

namespace Shortener.Analytics.Api.Application;

/// <summary>Records clicks into the analytics store, idempotently.</summary>
public interface IClickRecorder
{
    /// <summary>
    /// Records a click, unless its <see cref="LinkClicked.EventId"/> has
    /// already been recorded. Delivery is at-least-once, so duplicates are
    /// an expected, ordinary outcome rather than an error.
    /// </summary>
    /// <param name="clickEvent">The click to record.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if this was a new event; <see langword="false"/> if it was already recorded.</returns>
    Task<bool> TryRecordAsync(LinkClicked clickEvent, CancellationToken cancellationToken);
}
