using System;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// Tunable knobs shared by the WebSocket client and server transports
/// </summary>
/// <remarks>
/// Plain settings object so apps can map their own configuration onto it without Core
/// taking a dependency on any configuration framework. Sensible defaults match the
/// migration plan, apps override per hop
/// </remarks>
public sealed class WebSocketTransportOptions
{
    /// <summary>
    /// Use TLS, wss on the client and the expectation of https ingress on the server
    /// </summary>
    /// <remarks>
    /// Behind App Platform ingress the process itself speaks plaintext ws while the
    /// platform terminates TLS, so a self hosted server usually leaves this off and lets
    /// the client dial wss through the managed edge
    /// </remarks>
    public bool UseTls { get; set; }

    /// <summary>
    /// HTTP path the WebSocket upgrade is served and dialed on
    /// </summary>
    public string Path { get; set; } = ProtocolConstants.WebSocketPath;

    /// <summary>
    /// Subprotocol advertised and required on the handshake, the connect time version gate
    /// </summary>
    public string Subprotocol { get; set; } = ProtocolConstants.WebSocketSubprotocol;

    /// <summary>
    /// Largest inbound frame accepted before the session is closed
    /// </summary>
    public int MaxInboundBytes { get; set; } = ProtocolConstants.MaxAuthMessageBytes;

    /// <summary>
    /// Largest number of queued outbound messages before backpressure applies
    /// </summary>
    public int OutboundQueueCapacity { get; set; } = 256;

    /// <summary>
    /// Frame level keep alive interval, keeps intermediaries from idling the socket out
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = ProtocolConstants.WebSocketKeepAliveInterval;

    /// <summary>
    /// Negotiate permessage-deflate (RFC 7692) so repetitive list payloads ride compressed
    /// </summary>
    /// <remarks>
    /// The lists we push, music, characters, servers, badges, are repetitive text and
    /// compress well. Context takeover is deliberately disabled on both sides
    /// (see <see cref="DisableContextTakeover"/>) so a secret bearing frame such as login
    /// never shares a compression window with later attacker influenced text, which is the
    /// CRIME/BREACH class the migration plan calls out. Both peers must agree, so this rides
    /// the same shared options object
    /// </remarks>
    public bool EnablePerMessageDeflate { get; set; } = true;

    /// <summary>
    /// Compress each message on its own history rather than a shared sliding window
    /// </summary>
    /// <remarks>
    /// No context takeover trades a little ratio for the guarantee that one frame's plaintext
    /// cannot be probed through another frame's compressed size. Kept on by default because the
    /// wire carries a login password and the residual ratio on a single repetitive list is
    /// still good
    /// </remarks>
    public bool DisableContextTakeover { get; set; } = true;

    /// <summary>
    /// Reconnect the outgoing link automatically when it drops, client links only
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// First reconnect delay, doubled on each failure up to the maximum
    /// </summary>
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Ceiling for the exponential reconnect backoff
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns a shallow copy so a per hop override cannot mutate the shared defaults
    /// </summary>
    public WebSocketTransportOptions Clone() => (WebSocketTransportOptions)MemberwiseClone();
}
