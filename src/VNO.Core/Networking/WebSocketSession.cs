using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// One accepted WebSocket peer on the server, mirrors <see cref="TcpSession"/>
/// </summary>
/// <remarks>
/// The server creates and tracks these. Each wraps a pump with the close on overflow policy
/// so one slow peer is dropped rather than allowed to grow memory. <see cref="Completion"/>
/// lets the Kestrel request handler stay parked until the socket ends, which is how ASP.NET
/// keeps the underlying connection open
/// </remarks>
internal sealed class WebSocketSession : IAsyncDisposable
{
    private readonly WebSocketMessagePump _pump;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Creates a session around an accepted socket
    /// </summary>
    public WebSocketSession(
        string id,
        WebSocket socket,
        string remoteAddress,
        WebSocketTransportOptions options,
        ILogger logger)
    {
        Id = id;
        RemoteAddress = remoteAddress;
        _pump = new WebSocketMessagePump(socket, options, BackpressurePolicy.CloseOnOverflow, logger);
        _pump.MessageReceived += OnPumpMessage;
        _pump.Closed += OnPumpClosed;
    }

    /// <summary>
    /// Stable id of this session
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Remote address of the peer, resolved from forwarded headers when behind ingress
    /// </summary>
    public string RemoteAddress { get; }

    /// <summary>
    /// Completes when the session has fully closed
    /// </summary>
    public Task Completion => _completion.Task;

    /// <summary>
    /// Raised when this session receives a complete message
    /// </summary>
    public event Action<WebSocketSession, NetworkMessage>? MessageReceived;

    /// <summary>
    /// Raised when this session ends for any reason
    /// </summary>
    public event Action<WebSocketSession>? Closed;

    /// <summary>
    /// Starts the background pump
    /// </summary>
    public void Start() => _pump.Start();

    /// <summary>
    /// Sends a message to this peer
    /// </summary>
    public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken) =>
        _pump.SendAsync(message, cancellationToken).AsTask();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _pump.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "server closing", TimeSpan.FromSeconds(3))
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // teardown never throws to the caller
        }

        await _pump.DisposeAsync().ConfigureAwait(false);
        _completion.TrySetResult();
    }

    private void OnPumpMessage(NetworkMessage message) => MessageReceived?.Invoke(this, message);

    private void OnPumpClosed(string reason)
    {
        Closed?.Invoke(this);
        _completion.TrySetResult();
    }
}
