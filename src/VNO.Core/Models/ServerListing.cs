namespace VNO.Core.Models;

/// <summary>
/// One public game server entry from the master's directory
/// </summary>
/// <remarks>
/// Matches the fields of the legacy SDA server list packet, index, name,
/// address, port, description, and optional content download source
/// </remarks>
public sealed class ServerListing
{
    /// <summary>
    /// Position of the entry in the master's list
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Display name of the server
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Host name or address of the server
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// TCP port of the server
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// Server supplied description shown in the list
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Optional content pack download source
    /// </summary>
    public string ContentUrl { get; init; } = string.Empty;
}
