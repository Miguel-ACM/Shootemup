public enum RunDifficulty
{
    Easy = 0, Hard = 1, Normal = 2
}

public static class Difficulty
{
    // Keep the original numeric values for saved preferences and editor fixtures.
    public static RunDifficulty Current { get; private set; } = RunDifficulty.Normal;
    public static bool Easy => Current == RunDifficulty.Easy;
    public static bool Normal => Current == RunDifficulty.Normal;
    public static bool Hard => Current == RunDifficulty.Hard;
    public static float Health => Easy ? .85f : 1;
    public static float Tempo => Hard ? 1.08f : Easy ? .94f : 1;
    public static float ShotSpeed => Hard ? .82f : Easy ? .64f : .76f;
    public static float Density => Hard ? 1 : Easy ? .42f : .65f;
    public static float RestScale => Hard ? 1 : Easy ? 1.7f : 1.3f;
    public static float ScoreFactor => Hard ? 2 : Easy ? .5f : 1;
    public static float Damage => 1;
    public static int Dense(int count) => UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(count * Density));
    public static float Rest(float seconds) => seconds * RestScale;
    public static void Select(RunDifficulty difficulty)
    {
        Current = difficulty == RunDifficulty.Easy || difficulty == RunDifficulty.Hard ? difficulty : RunDifficulty.Normal;
    }
}
