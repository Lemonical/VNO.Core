using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace VNO.Core;

/// <summary>
/// Result of rolling a dice expression
/// </summary>
public sealed class DiceResult
{
    /// <summary>The expression that was rolled, normalized</summary>
    public required string Expression { get; init; }

    /// <summary>Each individual die face rolled</summary>
    public required IReadOnlyList<int> Rolls { get; init; }

    /// <summary>Flat modifier added to the sum</summary>
    public int Modifier { get; init; }

    /// <summary>Total of the rolls plus the modifier</summary>
    public int Total => Rolls.Sum() + Modifier;
}

/// <summary>
/// Rolls the NdM dice notation the legacy dice box used
/// </summary>
/// <remarks>
/// Ports the edit_dice roller from Form15. Accepts forms like "2d6", "d20", and
/// "3d6+2". Count defaults to one, an optional signed modifier is added to the
/// total. Invalid input yields null so callers can ignore a bad expression the
/// way the original silently did nothing
/// </remarks>
public static class DiceRoller
{
    private static readonly Regex Pattern = new(
        @"^\s*(\d*)\s*[dD]\s*(\d+)\s*([+-]\s*\d+)?\s*$", RegexOptions.Compiled);

    // guard rails so a pasted huge expression cannot hang the roller
    private const int MaxDice = 100;
    private const int MaxSides = 1000;

    /// <summary>
    /// Rolls the expression, returning null when it cannot be parsed
    /// </summary>
    public static DiceResult? Roll(string expression, Random? random = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var match = Pattern.Match(expression);
        if (!match.Success)
        {
            return null;
        }

        var count = match.Groups[1].Value.Length == 0
            ? 1
            : int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var sides = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (count is < 1 or > MaxDice || sides is < 1 or > MaxSides)
        {
            return null;
        }

        var modifier = 0;
        if (match.Groups[3].Success)
        {
            var raw = match.Groups[3].Value.Replace(" ", string.Empty);
            modifier = int.Parse(raw, CultureInfo.InvariantCulture);
        }

        var rng = random ?? Random.Shared;
        var rolls = new int[count];
        for (var i = 0; i < count; i++)
        {
            rolls[i] = rng.Next(1, sides + 1);
        }

        var normalized = modifier == 0
            ? $"{count}d{sides}"
            : $"{count}d{sides}{(modifier > 0 ? "+" : "-")}{Math.Abs(modifier)}";

        return new DiceResult { Expression = normalized, Rolls = rolls, Modifier = modifier };
    }
}
