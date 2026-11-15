using UnityEngine;

// Authored district conditions drawn from the run seed. Each one changes the scenery and
// brings its own wave parts; the district's identity, enemies and bullet palette stay intact.
public enum DistrictCondition
{
    Ordinary, Dusk, Night, Blizzard, Sandstorm,
    // District events (15 November 2026): each has its own hazard or wave rule.
    Thunderstorm, Minefield, DeepPassages, CandyRain, Eruption, SpearRain, RushHour,
    // Plains forest variety and Space's solar flare (15 November 2026).
    Forest, SolarFlare
}

public static class DistrictConditions
{
    // Debug and review tooling can pin a condition; null follows the seed.
    public static DistrictCondition? Forced
    {
        get; set;
    }

    public static DistrictCondition Current => For(LevelRun.Current, LevelRun.Seed);

    public static DistrictCondition For(LevelId level, int seed)
    {
        if (Forced.HasValue)
            return Allowed(level, Forced.Value) ? Forced.Value : DistrictCondition.Ordinary;
        int roll = Hash(seed, (int)level * 7349 + 811) % 100;
        switch (level)
        {
            case LevelId.Fields:
                return roll < 30 ? DistrictCondition.Ordinary : roll < 53 ? DistrictCondition.Dusk : roll < 75 ? DistrictCondition.Night : DistrictCondition.Forest;
            case LevelId.Snow:
                return roll < 50 ? DistrictCondition.Ordinary : DistrictCondition.Blizzard;
            case LevelId.Desert:
                return roll < 50 ? DistrictCondition.Ordinary : DistrictCondition.Sandstorm;
            case LevelId.Thunder:
                return roll < EventOdds ? DistrictCondition.Thunderstorm : DistrictCondition.Ordinary;
            case LevelId.Underwater:
                return roll < EventOdds ? DistrictCondition.Minefield : DistrictCondition.Ordinary;
            case LevelId.Tunnel:
                return roll < EventOdds ? DistrictCondition.DeepPassages : DistrictCondition.Ordinary;
            case LevelId.Candyland:
                return roll < EventOdds ? DistrictCondition.CandyRain : DistrictCondition.Ordinary;
            case LevelId.Hell:
                return roll < EventOdds ? DistrictCondition.Eruption : DistrictCondition.Ordinary;
            case LevelId.Heaven:
                return roll < EventOdds ? DistrictCondition.SpearRain : DistrictCondition.Ordinary;
            case LevelId.City:
                return roll < EventOdds ? DistrictCondition.RushHour : DistrictCondition.Ordinary;
            case LevelId.Space:
                return roll < EventOdds ? DistrictCondition.SolarFlare : DistrictCondition.Ordinary;
            default:
                return DistrictCondition.Ordinary;
        }
    }

    // Events are a sometimes treat, not the usual district (lowered from 45%).
    const int EventOdds = 35;

    public static LevelId Home(DistrictCondition condition)
    {
        switch (condition)
        {
            case DistrictCondition.Dusk:
            case DistrictCondition.Night:
            case DistrictCondition.Forest:
                return LevelId.Fields;
            case DistrictCondition.SolarFlare:
                return LevelId.Space;
            case DistrictCondition.Blizzard:
                return LevelId.Snow;
            case DistrictCondition.Sandstorm:
                return LevelId.Desert;
            case DistrictCondition.Thunderstorm:
                return LevelId.Thunder;
            case DistrictCondition.Minefield:
                return LevelId.Underwater;
            case DistrictCondition.DeepPassages:
                return LevelId.Tunnel;
            case DistrictCondition.CandyRain:
                return LevelId.Candyland;
            case DistrictCondition.Eruption:
                return LevelId.Hell;
            case DistrictCondition.SpearRain:
                return LevelId.Heaven;
            case DistrictCondition.RushHour:
                return LevelId.City;
            default:
                return LevelId.Void;
        }
    }

    public static string Label(DistrictCondition condition)
    {
        switch (condition)
        {
            case DistrictCondition.DeepPassages:
                return "DEEP PASSAGES";
            case DistrictCondition.CandyRain:
                return "CANDY RAIN";
            case DistrictCondition.SpearRain:
                return "SPEAR RAIN";
            case DistrictCondition.RushHour:
                return "RUSH HOUR";
            case DistrictCondition.SolarFlare:
                return "SOLAR FLARE";
            default:
                return condition.ToString().ToUpperInvariant();
        }
    }

    static readonly Sprite[] icons = new Sprite[System.Enum.GetValues(typeof(DistrictCondition)).Length];
    public static Sprite Icon(DistrictCondition condition)
    {
        int index = (int)condition;
        if (icons[index] != null)
            return icons[index];
        var texture = Resources.Load<Texture2D>("EncounterIcons/Condition" + condition);
        if (texture == null)
            throw new System.InvalidOperationException("Missing district condition icon: " + condition);
        return icons[index] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
    }

    public static bool Allowed(LevelId level, DistrictCondition condition)
    {
        return condition == DistrictCondition.Ordinary || Home(condition) == level;
    }

    // The low sun's side at dusk, or the side the wind blows from: -1 left, +1 right.
    // Scenery light, drifting weather and the condition's wave entrances all share it.
    public static int Side(LevelId level, int seed) => Hash(seed, (int)level * 4111 + 97) % 2 == 0 ? -1 : 1;
    public static int CurrentSide => Side(LevelRun.Current, LevelRun.Seed);

    public static bool Is(DistrictCondition condition) => Current == condition;

    // Ordinary falling snow drifts downwind in a blizzard: a gentle slant that rewards
    // reading the wind, about a sixth of the flakes' fall speed.
    public static Vector2 BlizzardWind => new Vector2(-CurrentSide * .32f, 0);

    static int Hash(int value, int salt)
    {
        unchecked
        {
            uint x = (uint)value ^ (uint)salt * 0x9e3779b9u;
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            return (int)((x ^ (x >> 16)) & 0x7fffffffu);
        }
    }
}
