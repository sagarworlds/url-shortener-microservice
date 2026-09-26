using Shortener.Contracts;

namespace Shortener.Analytics.Api.Application;

/// <summary>Handles the record-a-click use case.</summary>
public sealed class RecordClickHandler(IClickRecorder recorder)
{
    /// <summary>Records a click.</summary>
    /// <param name="clickEvent">The click to record.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if this was a new event; <see langword="false"/> for a duplicate.</returns>
    public Task<bool> HandleAsync(LinkClicked clickEvent, CancellationToken cancellationToken) =>
        recorder.TryRecordAsync(clickEvent, cancellationToken);
}
