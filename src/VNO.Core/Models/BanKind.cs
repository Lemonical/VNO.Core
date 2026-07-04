namespace VNO.Core.Models;

/// <summary>
/// What a ban record targets
/// </summary>
public enum BanKind
{
    /// <summary>
    /// Bans a single account name
    /// </summary>
    Account,

    /// <summary>
    /// Bans a network address
    /// </summary>
    IpAddress,
}
