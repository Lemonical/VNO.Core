namespace VNO.Core.Models;

/// <summary>
/// A room players can join, called an area in the legacy server
/// </summary>
/// <remarks>
/// The server admin window exposed an areas editor, this is the model behind it
/// </remarks>
public sealed class Area
{
    /// <summary>
    /// Stable id used in messages that reference an area
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Display name shown in the area list
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Background scene name the area shows by default
    /// </summary>
    public string Background { get; set; } = string.Empty;

    /// <summary>
    /// True when the area is locked and rejects new players
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// How many players the area allows, zero means no limit
    /// </summary>
    public int Capacity { get; set; }
}
