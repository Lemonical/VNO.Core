namespace VNO.Core.Models;

/// <summary>
/// A song that can be played in an area
/// </summary>
/// <remarks>
/// The server admin window had a music editor, the client could request a track
/// which the server then broadcast to the area
/// </remarks>
public sealed class MusicTrack
{
    /// <summary>
    /// Display name shown in the music list
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File name or url the client uses to fetch and play the track
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Length in seconds when known, used for cooldowns and looping
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// True when the client has the audio file locally, drives the legacy list
    /// coloring where tracks you have use the listbox_item color and tracks you
    /// are missing use the listbox_item_missing color
    /// </summary>
    public bool HasFile { get; set; }
}
