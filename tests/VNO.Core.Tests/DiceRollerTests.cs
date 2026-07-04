using System;
using VNO.Core;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Covers the NdM dice notation roller
/// </summary>
public sealed class DiceRollerTests
{
    [Theory]
    [InlineData("2d6", 2, 6)]
    [InlineData("d20", 1, 20)]
    [InlineData(" 3 D 10 ", 3, 10)]
    public void Rolls_the_requested_number_of_dice_in_range(string expr, int count, int sides)
    {
        var result = DiceRoller.Roll(expr, new Random(1));

        Assert.NotNull(result);
        Assert.Equal(count, result!.Rolls.Count);
        foreach (var roll in result.Rolls)
        {
            Assert.InRange(roll, 1, sides);
        }
    }

    [Fact]
    public void Applies_a_signed_modifier_to_the_total()
    {
        var result = DiceRoller.Roll("1d1+5");

        Assert.NotNull(result);
        // a one sided die always rolls 1, so the total is 1 + 5
        Assert.Equal(6, result!.Total);
        Assert.Equal("1d1+5", result.Expression);
    }

    [Fact]
    public void Negative_modifier_is_kept()
    {
        var result = DiceRoller.Roll("2d1-1");

        Assert.NotNull(result);
        Assert.Equal(1, result!.Total);
        Assert.Equal("2d1-1", result.Expression);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("2x6")]
    [InlineData("0d6")]
    [InlineData("2d0")]
    [InlineData("9999d6")]
    public void Rejects_bad_expressions(string expr)
    {
        Assert.Null(DiceRoller.Roll(expr));
    }

    [Fact]
    public void Is_deterministic_with_a_seeded_random()
    {
        var a = DiceRoller.Roll("5d20", new Random(42));
        var b = DiceRoller.Roll("5d20", new Random(42));

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a!.Rolls, b!.Rolls);
    }
}
