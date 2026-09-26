using Shortener.Contracts;

namespace Shortener.Redirect.Api.Application;

/// <summary>
/// Publishes <see cref="LinkClicked"/> events for successful redirects.
/// Synchronous and non-blocking: the redirect hot path must never wait on
/// the broker, and losing an occasional click is acceptable.
/// </summary>
public interface IClickPublisher
{
    /// <summary>Publishes a click event, best-effort.</summary>
    /// <param name="clickEvent">The click that occurred.</param>
    void Publish(LinkClicked clickEvent);
}
