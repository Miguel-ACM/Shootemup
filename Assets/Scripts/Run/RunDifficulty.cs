public enum RunDifficulty
{
    Easy = 0, Hard = 1, Normal = 2
}

// Only the three campaign difficulties are selectable or saved. Firing tiers belong to shooters.
public enum FiringTier { Easy, Normal, Hard, Extreme }

public static class Difficulty
{
    // Keep the original numeric values for saved preferences and editor fixtures.
    public static RunDifficulty Current { get; private set; } = RunDifficulty.Normal;
    public static bool Easy => Current == RunDifficulty.Easy;
    public static bool Normal => Current == RunDifficulty.Normal;
    public static bool Hard => Current == RunDifficulty.Hard;
    public static float Health => Easy ? .85f : 1;
    public static FiringTier Firing => Easy ? FiringTier.Easy : Hard ? FiringTier.Hard : FiringTier.Normal;
    public static FiringTier EliteFiring => Easy ? FiringTier.Normal : Hard ? FiringTier.Extreme : FiringTier.Hard;
    public static float Tempo => Firing.TempoScale();
    public static float ShotSpeed => Firing.SpeedScale();
    public static float Density => Firing.DensityScale();
    public static float RestScale => Firing.Rest(1);
    public static float TempoScale(this FiringTier tier) => tier switch
    { FiringTier.Easy => .94f, FiringTier.Hard => 1.08f, FiringTier.Extreme => 1.16f, _ => 1 };
    public static float SpeedScale(this FiringTier tier) => tier switch
    { FiringTier.Easy => .64f, FiringTier.Hard => .82f, FiringTier.Extreme => .90f, _ => .76f };
    static float DensityScale(this FiringTier tier) => tier switch
    { FiringTier.Easy => .42f, FiringTier.Hard => 1, FiringTier.Extreme => 1.2f, _ => .65f };
    public static float ScoreFactor => Hard ? 2 : Easy ? .5f : 1;
    public static float Damage => 1;
    // District pressure fills volleys toward, and a little past, Hard's authored count, and trims rests.
    public static int Dense(int count, float pressure = 1) => Firing.Dense(count, pressure);
    public static int Dense(this FiringTier tier, int count, float pressure = 1) =>
        UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(count * UnityEngine.Mathf.Min(tier == FiringTier.Extreme ? 1.5f : 1.25f, tier.DensityScale() * pressure)));
    public static float Rest(float seconds, float pressure = 1) => Firing.Rest(seconds, pressure);
    public static float Rest(this FiringTier tier, float seconds, float pressure = 1)
    {
        float scale = tier switch { FiringTier.Easy => 1.7f, FiringTier.Hard => 1, FiringTier.Extreme => .86f, _ => 1.3f };
        return seconds * scale / (1 + (pressure - 1) * .4f);
    }
    public static void Select(RunDifficulty difficulty)
    {
        Current = difficulty == RunDifficulty.Easy || difficulty == RunDifficulty.Hard ? difficulty : RunDifficulty.Normal;
    }
}
