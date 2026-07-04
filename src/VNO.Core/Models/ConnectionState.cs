namespace VNO.Core.Models;

/// <summary>
/// Lifecycle state of a network link
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Not connected and not trying to connect
    /// </summary>
    Disconnected,

    /// <summary>
    /// A connection attempt is in progress
    /// </summary>
    Connecting,

    /// <summary>
    /// Connected and ready to send and receive
    /// </summary>
    Connected,

    /// <summary>
    /// Shutting the link down in an orderly way
    /// </summary>
    Closing,

    /// <summary>
    /// The link failed, see the reported error for the reason
    /// </summary>
    Faulted,
}
