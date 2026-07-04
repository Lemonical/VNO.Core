using System;
using System.Threading;
using System.Threading.Tasks;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// A listening server that accepts many message clients at once
/// </summary>
/// <remarks>
/// This replaces the legacy TServerSocket used by the game server to host
/// players. Each accepted peer is a session identified by a string id
/// </remarks>
public interface IMessageServer : IAsyncDisposable
{
    /// <summary>
    /// True while the server is listening
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Number of connected sessions
    /// </summary>
    int SessionCount { get; }

    /// <summary>
    /// Raised when a peer connects
    /// </summary>
    event EventHandler<SessionEventArgs>? SessionConnected;

    /// <summary>
    /// Raised when a peer disconnects
    /// </summary>
    event EventHandler<SessionEventArgs>? SessionDisconnected;

    /// <summary>
    /// Raised when a message arrives from any session
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Starts listening on the given port
    /// </summary>
    Task StartAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening and closes all sessions
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Sends a message to one session
    /// </summary>
    Task SendToAsync(string sessionId, NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to every connected session
    /// </summary>
    Task BroadcastAsync(NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes one session
    /// </summary>
    Task DisconnectAsync(string sessionId);

    /// <summary>
    /// Gets the remote address of a session, empty when not found
    /// </summary>
    string GetRemoteAddress(string sessionId);
}
