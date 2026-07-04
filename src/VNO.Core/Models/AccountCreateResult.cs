namespace VNO.Core.Models;

/// <summary>
/// Outcome of an account creation attempt against the master
/// </summary>
/// <remarks>
/// Mirrors the legacy AS replies, OKCR for created, NOKCR for a taken name,
/// and INVCHAR for a name the master refused
/// </remarks>
public enum AccountCreateResult
{
    /// <summary>
    /// The account was created
    /// </summary>
    Created,

    /// <summary>
    /// The account name is already taken
    /// </summary>
    Taken,

    /// <summary>
    /// The master refused the name or password
    /// </summary>
    Invalid,

    /// <summary>
    /// There is no live link to the master
    /// </summary>
    NotConnected,

    /// <summary>
    /// The master did not answer in time
    /// </summary>
    TimedOut,
}
