namespace VNO.Core.Models;

/// <summary>
/// Outcome of an account login attempt against the master
/// </summary>
/// <remarks>
/// Mirrors the legacy AS replies, VNAL for granted, VNODE for denied, and
/// VNOBD for a banned account
/// </remarks>
public enum MasterLoginResult
{
    /// <summary>
    /// The credentials matched and the session is logged in
    /// </summary>
    Granted,

    /// <summary>
    /// The account does not exist or the password is wrong
    /// </summary>
    Denied,

    /// <summary>
    /// The account is banned
    /// </summary>
    Banned,

    /// <summary>
    /// There is no live link to the master
    /// </summary>
    NotConnected,

    /// <summary>
    /// The master did not answer in time
    /// </summary>
    TimedOut,
}
