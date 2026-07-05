namespace VNO.Core.Networking;

/// <summary>
/// What a single writer does when its bounded outbound queue is full
/// </summary>
/// <remarks>
/// Raw TCP hid slow consumers behind the OS send buffer. The WebSocket path makes the
/// choice explicit so a slow reader cannot grow memory without bound
/// </remarks>
public enum BackpressurePolicy
{
    /// <summary>
    /// Drop the connection when the queue overflows, used by server sessions where one
    /// slow peer must never threaten the whole process
    /// </summary>
    CloseOnOverflow,

    /// <summary>
    /// Await room in the queue, used by a long lived outgoing link where the caller can
    /// tolerate being slowed rather than disconnected
    /// </summary>
    Wait,
}
