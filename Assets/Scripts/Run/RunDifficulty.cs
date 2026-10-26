public enum RunDifficulty
{
    Easy, Hard
}

public static class Difficulty
{
    public static RunDifficulty Current { get; private set; } = RunDifficulty.Hard;
    public static bool Easy => Current == RunDifficulty.Easy;
    public static float Health => Easy ? .8f : 1;
    public static float Tempo => Easy ? .96f : 1;
    // Slow hostile travel leaves time to read geometry on both difficulties.
    // Counts and firing scores are separately authored in each enemy and wave.
    public static float ShotSpeed => Easy ? .61f : .76f;
    public static float Damage => Easy ? .65f : 1;
    public static void Select(RunDifficulty difficulty)
    {
        Current = difficulty;
    }
}
