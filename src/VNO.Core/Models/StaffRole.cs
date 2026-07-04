using System;

namespace VNO.Core.Models;

/// <summary>
/// Powers a staff account can hold
/// </summary>
/// <remarks>
/// The values are flags so an account can hold more than one role at once, for
/// example an owner who is also an animator
/// </remarks>
[Flags]
public enum StaffRole
{
    /// <summary>
    /// No special powers
    /// </summary>
    None = 0,

    /// <summary>
    /// Can kick, mute, and ban players
    /// </summary>
    Moderator = 1,

    /// <summary>
    /// Can change player stats and give items
    /// </summary>
    Animator = 2,

    /// <summary>
    /// Full control of the server
    /// </summary>
    Owner = 4,
}
