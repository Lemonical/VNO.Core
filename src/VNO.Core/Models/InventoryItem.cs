namespace VNO.Core.Models;

/// <summary>
/// An item the animator can grant to a player
/// </summary>
/// <remarks>
/// The animator interface in Form7 had a give item action, this is the item it
/// referred to
/// </remarks>
public sealed class InventoryItem
{
    /// <summary>
    /// Stable id used in give item messages
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Display name shown to players
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description of what the item does
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
