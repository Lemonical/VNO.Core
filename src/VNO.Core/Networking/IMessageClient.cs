using System;
using System.Threading;
using System.Threading.Tasks;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// An outgoing connection that sends and receives VNO messages
/// </summary>
/// <remarks>
/// Both the game client to server link and the link to the auth server use this
/// shape. Implementations own a single socket and a background receive loop
/// </remarks>
public interface IMessageClient : IAsyncDisposable
{
    /// <summary>
    /// Current state of the link
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Raised when a message arrives
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Raised when the link changes state
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Connects to the given host and port
    /// </summary>
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message, no op when not connected
    /// </summary>
    Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the link in an orderly way
    /// </summary>
    Task DisconnectAsync();
}
