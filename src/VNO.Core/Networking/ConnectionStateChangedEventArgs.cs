using System;
using VNO.Core.Models;

namespace VNO.Core.Networking;

/// <summary>
/// Reports that a link changed its connection state
/// </summary>
public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates the event data
    /// </summary>
    public ConnectionStateChangedEventArgs(ConnectionState state, string? reason = null)
    {
        State = state;
        Reason = reason;
    }

    /// <summary>
    /// The new state of the link
    /// </summary>
    public ConnectionState State { get; }

    /// <summary>
    /// Optional human readable reason, set when faulted or closing
    /// </summary>
    public string? Reason { get; }
}
