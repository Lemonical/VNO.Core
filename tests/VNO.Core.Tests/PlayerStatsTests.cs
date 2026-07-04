using System.Collections.Generic;
using VNO.Core.Models;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Covers that player stats raise change notifications so a bound gauge updates
/// </summary>
public sealed class PlayerStatsTests
{
    [Fact]
    public void Setting_health_raises_property_changed()
    {
        var stats = new PlayerStats();
        var changed = new List<string?>();
        stats.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        stats.Health = 42;

        Assert.Contains(nameof(PlayerStats.Health), changed);
    }

    [Fact]
    public void Setting_the_same_value_does_not_notify()
    {
        var stats = new PlayerStats { Mana = 10 };
        var count = 0;
        stats.PropertyChanged += (_, _) => count++;

        stats.Mana = 10;

        Assert.Equal(0, count);
    }
}
