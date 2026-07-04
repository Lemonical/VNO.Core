namespace VNO.Core.Models;

/// <summary>
/// A staff account allowed to use moderator or animator powers
/// </summary>
/// <remarks>
/// The legacy server admin window had mods and animators editors, this is the
/// account behind those lists. Passwords are never stored in clear text, only a
/// hash is kept
/// </remarks>
public sealed class ModeratorAccount
{
    /// <summary>
    /// Login name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the password, the clear password is never stored
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// The powers this account holds
    /// </summary>
    public StaffRole Role { get; set; } = StaffRole.Moderator;
}
