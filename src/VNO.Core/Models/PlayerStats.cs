using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VNO.Core.Models;

/// <summary>
/// Hit point and mana values an animator can change for a player
/// </summary>
/// <remarks>
/// The animator interface in Form7 set current and max hit points and mana and
/// chose font colors for the readout, those are gathered here. The stage draws two
/// opposing courtroom sides, so there is a left and a right value for both hit
/// points and mana. It raises change notifications so a bound gauge updates live
/// when an animator edits a stat. This uses <see cref="INotifyPropertyChanged"/>
/// from the base class library, not a UI framework, so VNO.Core stays UI free
/// </remarks>
public sealed class PlayerStats : INotifyPropertyChanged
{
    private int _health;
    private int _maxHealth;
    private int _healthRight;
    private int _maxHealthRight;
    private int _mana;
    private int _maxMana;
    private int _manaRight;
    private int _maxManaRight;
    private string _healthColor = "#FFFFFFFF";
    private string _manaColor = "#FFFFFFFF";

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Current hit points of the left side
    /// </summary>
    public int Health
    {
        get => _health;
        set => Set(ref _health, value);
    }

    /// <summary>
    /// Maximum hit points of the left side
    /// </summary>
    public int MaxHealth
    {
        get => _maxHealth;
        set => Set(ref _maxHealth, value);
    }

    /// <summary>
    /// Current hit points of the right side, the opposing courtroom bar
    /// </summary>
    public int HealthRight
    {
        get => _healthRight;
        set => Set(ref _healthRight, value);
    }

    /// <summary>
    /// Maximum hit points of the right side
    /// </summary>
    public int MaxHealthRight
    {
        get => _maxHealthRight;
        set => Set(ref _maxHealthRight, value);
    }

    /// <summary>
    /// Current mana of the left side
    /// </summary>
    public int Mana
    {
        get => _mana;
        set => Set(ref _mana, value);
    }

    /// <summary>
    /// Maximum mana of the left side
    /// </summary>
    public int MaxMana
    {
        get => _maxMana;
        set => Set(ref _maxMana, value);
    }

    /// <summary>
    /// Current mana of the right side
    /// </summary>
    public int ManaRight
    {
        get => _manaRight;
        set => Set(ref _manaRight, value);
    }

    /// <summary>
    /// Maximum mana of the right side
    /// </summary>
    public int MaxManaRight
    {
        get => _maxManaRight;
        set => Set(ref _maxManaRight, value);
    }

    /// <summary>
    /// Color used to draw the health readout, stored as an argb hex string
    /// </summary>
    public string HealthColor
    {
        get => _healthColor;
        set => Set(ref _healthColor, value);
    }

    /// <summary>
    /// Color used to draw the mana readout, stored as an argb hex string
    /// </summary>
    public string ManaColor
    {
        get => _manaColor;
        set => Set(ref _manaColor, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
