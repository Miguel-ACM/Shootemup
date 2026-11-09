using System;
using System.Collections.Generic;

/// <summary>
/// The hand-picked default tone of each enemy's attacks. Enemies that fire both tones
/// switch per attack with UseTone; bosses choose a tone for every card and accent.
/// Unlisted enemies fire bright.
/// </summary>
public static class EnemyTones
{
    // Lasers are always bright and never absorbed, so laser-only craft are not listed.
    static readonly Dictionary<Type, HostileTone> defaults = new Dictionary<Type, HostileTone>
    {
        // Plains: aimed arrows and tank bursts are bright; furrows, reeds and bombs are dark.
        { typeof(ScoutInterceptor), HostileTone.Bright },
        { typeof(PatrolGunship), HostileTone.Bright },
        { typeof(MeadowTank), HostileTone.Bright },
        { typeof(HeavyBomber), HostileTone.Bright },
        { typeof(HarrowBomber), HostileTone.Dark },
        { typeof(ReedSkimmer), HostileTone.Dark },
        { typeof(FieldsHeavyWing), HostileTone.Dark },

        // Snow: re-aimed chevrons and aimed shells are bright; sliding sheets, hanging
        // icicles and falling snow are dark.
        { typeof(ColdWeatherFighter), HostileTone.Bright },
        { typeof(FrostAuger), HostileTone.Bright },
        { typeof(ShieldTransport), HostileTone.Bright },
        { typeof(AvalancheBomber), HostileTone.Bright },
        { typeof(SiegeGunship), HostileTone.Dark },
        { typeof(HailLantern), HostileTone.Dark },
        { typeof(IcicleWeaver), HostileTone.Dark },
        { typeof(SnowSiegeCraft), HostileTone.Dark },
    };

    public static HostileTone Default(Type enemy) => defaults.TryGetValue(enemy, out var tone) ? tone : HostileTone.Bright;
}
