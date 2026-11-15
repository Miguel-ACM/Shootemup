using UnityEngine;

// Authored district conditions drawn from the run seed. Each one changes the scenery and
// brings its own wave parts; the district's identity, enemies and bullet palette stay intact.
public enum DistrictCondition
{
    Ordinary, Dusk, Night, Blizzard, Sandstorm
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
                return roll < 34 ? DistrictCondition.Ordinary : roll < 67 ? DistrictCondition.Dusk : DistrictCondition.Night;
            case LevelId.Snow:
                return roll < 50 ? DistrictCondition.Ordinary : DistrictCondition.Blizzard;
            case LevelId.Desert:
                return roll < 50 ? DistrictCondition.Ordinary : DistrictCondition.Sandstorm;
            default:
                return DistrictCondition.Ordinary;
        }
    }

    public static bool Allowed(LevelId level, DistrictCondition condition)
    {
        switch (condition)
        {
            case DistrictCondition.Ordinary:
                return true;
            case DistrictCondition.Dusk:
            case DistrictCondition.Night:
                return level == LevelId.Fields;
            case DistrictCondition.Blizzard:
                return level == LevelId.Snow;
            case DistrictCondition.Sandstorm:
                return level == LevelId.Desert;
            default:
                return false;
        }
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
