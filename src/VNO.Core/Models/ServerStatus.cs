namespace VNO.Core.Models;

/// <summary>
/// Whether the game host is accepting players
/// </summary>
/// <remarks>
/// Mirrors the server admin status bar in the legacy Form3 which showed
/// OFFLINE or ONLINE and a public flag
/// </remarks>
public enum ServerStatus
{
    /// <summary>
    /// Not listening, no players can join
    /// </summary>
    Offline,

    /// <summary>
    /// Listening and accepting players
    /// </summary>
    Online,
}
