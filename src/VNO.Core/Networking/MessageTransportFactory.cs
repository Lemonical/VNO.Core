using Microsoft.Extensions.Logging;

namespace VNO.Core.Networking;

/// <summary>
/// Builds the concrete transport for a selected <see cref="Transport"/>
/// </summary>
/// <remarks>
/// One place picks TCP or WebSocket so every app selects the same way from config during the
/// dual transport window. Core stays free of any DI framework, the apps call these from their
/// own container wiring
/// </remarks>
public static class MessageTransportFactory
{
    /// <summary>
    /// Creates a listening server for the selected transport
    /// </summary>
    public static IMessageServer CreateServer(
        Transport transport,
        ILoggerFactory loggerFactory,
        WebSocketTransportOptions? options = null) =>
        transport == Transport.WebSocket
            ? new WebSocketMessageServer(loggerFactory, options)
            : new TcpMessageServer(loggerFactory.CreateLogger<TcpMessageServer>());

    /// <summary>
    /// Creates an outgoing client for the selected transport
    /// </summary>
    public static IMessageClient CreateClient(
        Transport transport,
        ILoggerFactory loggerFactory,
        WebSocketTransportOptions? options = null) =>
        transport == Transport.WebSocket
            ? new WebSocketMessageClient(loggerFactory.CreateLogger<WebSocketMessageClient>(), options)
            : new TcpMessageClient(loggerFactory.CreateLogger<TcpMessageClient>());
}
