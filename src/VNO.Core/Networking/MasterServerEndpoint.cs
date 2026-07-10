namespace VNO.Core.Networking;

/// <summary>
/// Defines the single public endpoint used to reach the VNO Master service.
/// </summary>
/// <remarks>
/// DigitalOcean App Platform terminates TLS on port 443 and forwards the WebSocket
/// connection to Master. Client and Server consume these constants directly so a
/// stale local configuration file cannot send either application to another host.
/// </remarks>
public static class MasterServerEndpoint
{
    /// <summary>
    /// Public DNS name of the Master service.
    /// </summary>
    public const string Host = "vno-master-rjrun.ondigitalocean.app";

    /// <summary>
    /// Public TLS port exposed by App Platform.
    /// </summary>
    public const int Port = 443;

    /// <summary>
    /// Transport used by the public Master service.
    /// </summary>
    public const Transport Transport = global::VNO.Core.Networking.Transport.WebSocket;

    /// <summary>
    /// Whether the public WebSocket connection uses TLS.
    /// </summary>
    public const bool UseTls = true;
}
