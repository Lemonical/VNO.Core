using System;

namespace VNO.Core.Models;

/// <summary>
/// A connected player as the server and moderators see them
/// </summary>
/// <remarks>
/// Combines the identity, character choice, and moderation flags that the legacy
/// moderator interface in Form1 acted on with kick, mute, and ban
/// </remarks>
public sealed class ChatUser
{
    /// <summary>
    /// Stable id assigned by the server for this session
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Account or display name shown in user lists
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Network address of the player, used by IP based moderation
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Hardware id the client reports, used to track ban evaders
    /// </summary>
    public string HardwareId { get; set; } = string.Empty;

    /// <summary>
    /// The character the player is currently using
    /// </summary>
    public string Character { get; set; } = string.Empty;

    /// <summary>
    /// Id of the area the player is in
    /// </summary>
    public int AreaId { get; set; }

    /// <summary>
    /// True when the player may not send chat
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// True when the player may change the music, revoked by a moderator DJ off
    /// </summary>
    public bool IsDj { get; set; } = true;

    /// <summary>
    /// True when a moderator isolated the player, their chat only echoes back to
    /// themselves so they cannot be heard by the rest of the area
    /// </summary>
    public bool IsIsolated { get; set; }

    /// <summary>
    /// True when the player holds a staff role
    /// </summary>
    public bool IsModerator { get; set; }

    /// <summary>
    /// When the session connected, in coordinated universal time
    /// </summary>
    public DateTimeOffset ConnectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// A short label for lists, such as the moderator user list in Form1
    /// </summary>
    public string DisplayLabel =>
        string.IsNullOrEmpty(Character)
            ? $"[{Id}] {Name}"
            : $"[{Id}] {Name} as {Character}";
}
