using System;

namespace VNO.Core.Models;

/// <summary>
/// A ban record covering either an account or an address
/// </summary>
/// <remarks>
/// The legacy server kept separate ban and ipban lists, this single record
/// carries a kind so both lists share one model and one store
/// </remarks>
public sealed class BanEntry
{
    /// <summary>
    /// Whether this ban targets an account name or a network address
    /// </summary>
    public BanKind Kind { get; init; }

    /// <summary>
    /// The banned value, a user name or an address depending on the kind
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Reason shown to staff and recorded for audits
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Who placed the ban
    /// </summary>
    public string PlacedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the ban was placed, in coordinated universal time
    /// </summary>
    public DateTimeOffset PlacedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the ban expires, null means it never expires
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// True when the ban is still in effect at the given time
    /// </summary>
    public bool IsActiveAt(DateTimeOffset moment) =>
        ExpiresAt is null || ExpiresAt.Value > moment;
}
