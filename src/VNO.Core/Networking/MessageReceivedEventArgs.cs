using System;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// Carries a message that arrived on a connection
/// </summary>
public sealed class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Creates the event data
    /// </summary>
    public MessageReceivedEventArgs(string sessionId, NetworkMessage message)
    {
        SessionId = sessionId;
        Message = message;
    }

    /// <summary>
    /// Id of the session the message came from, empty for a plain client link
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// The message that was received
    /// </summary>
    public NetworkMessage Message { get; }
}
