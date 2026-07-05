namespace VNO.Core.Networking;

/// <summary>
/// Which wire transport a message client or server uses
/// </summary>
/// <remarks>
/// Both implementations satisfy the same <see cref="IMessageClient"/> and
/// <see cref="IMessageServer"/> contracts, so a single binary can run either one and
/// pick it from configuration during the transition from TCP to WebSocket
/// </remarks>
public enum Transport
{
    /// <summary>
    /// The legacy delimited TCP stream, retained through the dual transport window
    /// </summary>
    Tcp,

    /// <summary>
    /// WebSocket over HTTP, one message per frame, cloud and browser reachable
    /// </summary>
    WebSocket,
}
