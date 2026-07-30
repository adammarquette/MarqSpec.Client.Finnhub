using System.Net.WebSockets;

namespace MarqSpec.Client.Finnhub;

/// <summary>
/// The websocket transport the quote stream talks to — a seam over <see cref="ClientWebSocket"/>.
/// </summary>
/// <remarks>
/// It exists so the stream's <b>behaviour</b> is testable without a network: the subscription cap, and — the one
/// that matters — that a reconnect <b>re-subscribes the existing symbol set</b>. A reconnect that comes back
/// subscribed to nothing is silent: the socket is healthy, the process is running, and no ticks arrive. That is
/// exactly the failure a live-only test cannot reach on demand.
/// </remarks>
public interface IFinnhubWebSocket : IDisposable
{
    /// <summary>Whether the socket is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Connects to <paramref name="uri"/>.</summary>
    /// <param name="uri">The websocket endpoint, token included.</param>
    /// <param name="cancellationToken">Cancels the connect.</param>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>Sends one UTF-8 text frame.</summary>
    /// <param name="text">The frame payload.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    Task SendAsync(string text, CancellationToken cancellationToken);

    /// <summary>
    /// Receives the next text frame, or <see langword="null"/> when the socket closed.
    /// </summary>
    /// <param name="cancellationToken">Cancels the receive.</param>
    /// <returns>The frame payload, or <see langword="null"/> on close.</returns>
    Task<string?> ReceiveAsync(CancellationToken cancellationToken);
}
