using UnityEngine;

public enum LevelId
{
    Fields, Tunnel, Snow, Thunder, City, Desert, Candyland, Space, Underwater, Hell, Heaven, Void
}

public static class LevelRun
{
    public static LevelId Current { get; private set; } = LevelId.Fields;
    public static string CurrentName => Current == LevelId.Fields ? "Plains" : Current.ToString();
    public static int Seed { get; private set; } = 17381;
    public static int Stage { get; private set; } = 1;
    public static int Loop { get; private set; } = 1;
    public static bool ReturnsToPlains => false;
    public static bool FinalDistrict => Current == LevelId.Void;
    public static LevelId? AutomaticNext => Tier(Current) == 5 ? LevelId.Void : (LevelId?)null;
    // Health keeps growing after the speed caps preserve readable patterns.
    public static float EnemyHealthScale => (1 + (Tier(Current) - 1) * .16f + (Loop - 1) * .48f) * Difficulty.Health;
    public static float EnemyTempoScale => Mathf.Min(1.3f, 1 + (Tier(Current) - 1) * .04f + (Loop - 1) * .05f) * Difficulty.Tempo;
    // Fuller volleys and shorter rests where a district measured lighter than the tier before it
    // (MeasureDifficultyCurve, ArtSource/DifficultyCurveReview). Geometry and identity stay authored.
    public static float WavePressure => DistrictPressure(Current, false) * (1 + (Loop - 1) * .1f);
    public static float BossPressure => DistrictPressure(Current, true) * (1 + (Loop - 1) * .1f);
    static float DistrictPressure(LevelId level, bool boss)
    {
        switch (level)
        {
            case LevelId.Snow:
                return boss ? 1.15f : 1;
            case LevelId.City:
                return boss ? 1 : 1.1f;
            case LevelId.Thunder:
                return boss ? 1.35f : 1.4f;
            case LevelId.Tunnel:
                return boss ? 1.3f : 1.35f;
            case LevelId.Desert:
                return boss ? 1.45f : 1.2f;
            case LevelId.Candyland:
                return boss ? 1.15f : 1.25f;
            case LevelId.Space:
                return boss ? 1 : 1.1f;
            case LevelId.Underwater:
                return boss ? 1.15f : 1.35f;
            case LevelId.Hell:
                return 1.3f;
            case LevelId.Heaven:
                return boss ? 1.15f : 1.5f;
            case LevelId.Void:
                return boss ? 1.65f : 1;
            default:
                return 1;
        }
    }
    public static float EnemyShotSpeedScale => Mathf.Min(1.18f, 1 + (Loop - 1) * .035f) * Difficulty.ShotSpeed;
    public static int Tier(LevelId level)
    {
        switch (level)
        {
            case LevelId.Fields:
                return 1;
            case LevelId.Snow:
            case LevelId.City:
                return 2;
            case LevelId.Thunder:
            case LevelId.Tunnel:
            case LevelId.Desert:
            case LevelId.Candyland:
                return 3;
            case LevelId.Space:
            case LevelId.Underwater:
                return 4;
            case LevelId.Hell:
            case LevelId.Heaven:
                return 5;
            case LevelId.Void:
                return 6;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(level));
        }
    }
    // Explicit starts are used by environment/boss previews; new games use Plains.
    public static void Reset(LevelId first = LevelId.Fields)
    {
        Reset(first, Random.Range(1, 10000000));
    }

    public static void Reset(LevelId first, int seed)
    {
        Stage = Tier(first);
        Loop = 1;
        Current = first;
        Seed = Mathf.Max(1, seed & int.MaxValue);
    }

    public static void Advance(LevelId next)
    {
        bool valid = System.Array.IndexOf(GetOptions(), next) >= 0;
        if (!valid)
            throw new System.InvalidOperationException("Invalid district progression");
        if (next == LevelId.Fields)
            Loop++;
        Current = next;
        Stage++;
    }

    public static void GetChoices(out LevelId left, out LevelId right)
    {
        if (FinalDistrict)
            throw new System.InvalidOperationException("Void completes the campaign");
        var options = GetOptions();
        left = options[0];
        right = options[options.Length - 1];
    }

    // Tooling can inspect later-loop scaling without inventing a campaign route past the finale.
    public static void Preview(LevelId level, int seed, int loop)
    {
        Reset(level, seed);
        Loop = Mathf.Max(1, loop);
        Stage = Tier(level) + (Loop - 1) * 6;
    }

    public static LevelId[] GetOptions()
    {
        if (FinalDistrict)
            return System.Array.Empty<LevelId>();
        if (Tier(Current) == 5)
            return new[] { LevelId.Void };
        if (Tier(Current) == 4)
            return new[] { LevelId.Hell, LevelId.Heaven };
        if (Tier(Current) == 3)
            return new[] { LevelId.Space, LevelId.Underwater };
        if (Current == LevelId.Snow)
            return new[] { LevelId.Desert, LevelId.Thunder };
        if (Current == LevelId.City)
            return new[] { LevelId.Tunnel, LevelId.Candyland };
        return new[] { LevelId.Snow, LevelId.City };
    }

    public static Color Accent(LevelId level)
    {
        switch (level)
        {
            case LevelId.Fields:
                return new Color(.54f, .77f, .43f);
            case LevelId.Tunnel:
                return new Color(.95f, .65f, .30f);
            case LevelId.Snow:
                return new Color(.65f, .88f, .98f);
            case LevelId.Thunder:
                return new Color(.68f, .60f, .94f);
            case LevelId.Desert:
                return new Color(.88f, .61f, .29f);
            case LevelId.Candyland:
                return new Color(.95f, .48f, .72f);
            case LevelId.Space:
                return new Color(.59f, .69f, .96f);
            case LevelId.Underwater:
                return new Color(.20f, .71f, .84f);
            case LevelId.Hell:
                return new Color(.96f, .28f, .17f);
            case LevelId.Heaven:
                return new Color(1, .79f, .29f);
            case LevelId.Void:
                return new Color(.66f, .32f, .97f);
            default:
                return new Color(.95f, .76f, .42f);
        }
    }

    public static string Description(LevelId level)
    {
        switch (level)
        {
            case LevelId.Fields:
                return "THE RIVER VALLEY";
            case LevelId.Tunnel:
                return "BENEATH THE SURFACE";
            case LevelId.Snow:
                return "WHITEOUT";
            case LevelId.Thunder:
                return "INTO THE STORM";
            case LevelId.Desert:
                return "THE BURIED MACHINES";
            case LevelId.Candyland:
                return "SUGAR RUSH";
            case LevelId.Space:
                return "THE SILENT ORBIT";
            case LevelId.Underwater:
                return "THE SUNKEN FLEET";
            case LevelId.Hell:
                return "THE LAST DESCENT";
            case LevelId.Heaven:
                return "THE SHATTERED FIRMAMENT";
            case LevelId.Void:
                return "BEYOND THE LAST LIGHT";
            default:
                return "THE CONCRETE CANYONS";
        }
    }
}
