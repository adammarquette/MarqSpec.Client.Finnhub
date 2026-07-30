using System.Net.WebSockets;
using System.Text;

namespace MarqSpec.Client.Finnhub;

/// <summary>
/// The production <see cref="IFinnhubWebSocket"/> — a thin wrapper over <see cref="ClientWebSocket"/>.
/// </summary>
/// <remarks>
/// Deliberately thin: it owns framing and buffer reassembly and <b>no policy</b>. Every decision worth testing —
/// the subscription cap, re-subscribing after a drop, what a malformed frame does — lives in
/// <see cref="FinnhubQuoteStream"/>, above this seam, where it can be driven without a network.
/// </remarks>
public sealed class ClientWebSocketTransport : IFinnhubWebSocket
{
    private readonly ClientWebSocket _socket = new();
    private bool _disposed;

    /// <inheritdoc />
    public bool IsOpen => _socket.State == WebSocketState.Open;

    /// <inheritdoc />
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _socket.ConnectAsync(uri, cancellationToken);

    /// <inheritdoc />
    public Task SendAsync(string text, CancellationToken cancellationToken) =>
        _socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);

    /// <inheritdoc />
    public async Task<string?> ReceiveAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        StringBuilder message = new();

        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }
            catch (WebSocketException)
            {
                // A dropped connection reads as "closed" to the caller, which reconnects and re-subscribes. It is
                // not an error to propagate: a feed that ends the process on a transient drop is worse than one
                // that reconnects.
                return null;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                return message.ToString();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _socket.Dispose();
    }
}
