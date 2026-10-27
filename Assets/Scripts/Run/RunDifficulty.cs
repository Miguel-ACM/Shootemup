public enum RunDifficulty
{
    Easy, Hard
}

public static class Difficulty
{
    public static RunDifficulty Current { get; private set; } = RunDifficulty.Hard;
    public static bool Easy => Current == RunDifficulty.Easy;
    public static bool Hard => Current == RunDifficulty.Hard;
    public static float Health => 1;
    public static float Tempo => Hard ? 1.08f : 1;
    // Slow hostile travel leaves time to read geometry on both difficulties.
    // Counts and firing scores are separately authored in each enemy and wave.
    public static float ShotSpeed => Hard ? .82f : .76f;
    public static float Damage => 1;
    public static void Select(RunDifficulty difficulty)
    {
        Current = difficulty;
    }
}
