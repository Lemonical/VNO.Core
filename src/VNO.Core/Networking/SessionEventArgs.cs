using System;

namespace VNO.Core.Networking;

/// <summary>
/// Reports that a server side session connected or disconnected
/// </summary>
public sealed class SessionEventArgs : EventArgs
{
    /// <summary>
    /// Creates the event data
    /// </summary>
    public SessionEventArgs(string sessionId, string remoteAddress)
    {
        SessionId = sessionId;
        RemoteAddress = remoteAddress;
    }

    /// <summary>
    /// Id of the session that changed
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Network address of the remote peer
    /// </summary>
    public string RemoteAddress { get; }
}
