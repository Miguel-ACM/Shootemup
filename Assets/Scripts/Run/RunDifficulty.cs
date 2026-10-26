public enum RunDifficulty { Easy, Hard }

public static class Difficulty
{
    public static RunDifficulty Current { get; private set; } = RunDifficulty.Hard;
    public static bool Easy => Current == RunDifficulty.Easy;
    public static float Health => Easy ? .8f : 1;
    public static float Tempo => Easy ? .88f : 1;
    public static float ShotSpeed => Easy ? .8f : 1;
    public static float Damage => Easy ? .65f : 1;
    public static void Select(RunDifficulty difficulty) { Current = difficulty; }
}
