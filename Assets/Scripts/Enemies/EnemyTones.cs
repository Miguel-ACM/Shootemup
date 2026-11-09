using System;
using System.Collections.Generic;

/// <summary>
/// The hand-picked default tone of each enemy's attacks. Enemies that fire both tones
/// switch per attack with UseTone; bosses choose a tone for every card and accent.
/// Unlisted enemies fire bright.
/// </summary>
public static class EnemyTones
{
    static readonly Dictionary<Type, HostileTone> defaults = new Dictionary<Type, HostileTone>
    {
    };

    public static HostileTone Default(Type enemy) => defaults.TryGetValue(enemy, out var tone) ? tone : HostileTone.Bright;
}
