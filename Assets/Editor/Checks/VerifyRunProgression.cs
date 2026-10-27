using System;

// Pure route/difficulty checks; no gameplay session is required.
public static class VerifyRunProgression
{
    public static void Run()
    {
        var originalDifficulty = Difficulty.Current;
        Difficulty.Select(RunDifficulty.Hard);
        try
        {
            Check((int)LevelId.Fields == 0 && (int)LevelId.Tunnel == 1 && (int)LevelId.Snow == 2 && (int)LevelId.Thunder == 3 &&
                (int)LevelId.City == 4 && (int)LevelId.Desert == 5 && (int)LevelId.Candyland == 6 && (int)LevelId.Space == 7, "Serialized district IDs changed");
            foreach (var middle in new[] { LevelId.Snow, LevelId.City })
                foreach (var third in middle == LevelId.Snow ? new[] { LevelId.Desert, LevelId.Thunder } : new[] { LevelId.Tunnel, LevelId.Candyland })
                    foreach (var extension in new[] { LevelId.Space, LevelId.Underwater })
                    {
                        LevelRun.Reset();
                        int seed = LevelRun.Seed;
                        float previousHealth = 0, previousTempo = 0, previousShots = 0;
                        for (int loop = 1; loop <= 50; loop++)
                        {
                            Check(LevelRun.Loop == loop && LevelRun.Stage == (loop - 1) * 5 + 1, "Loop or stage advanced incorrectly");
                            Check(LevelRun.EnemyHealthScale > previousHealth && LevelRun.EnemyTempoScale >= previousTempo && LevelRun.EnemyShotSpeedScale >= previousShots, "Run difficulty decreased");
                            Check(LevelRun.EnemyTempoScale <= 1.3f * Difficulty.Tempo + .0001f && LevelRun.EnemyShotSpeedScale <= 1.18f * Difficulty.ShotSpeed + .0001f, "Readability speed caps exceeded");
                            if (loop == 2)
                                Check(Near(LevelRun.EnemyHealthScale, 1.48f) && Near(LevelRun.EnemyTempoScale, 1.05f * Difficulty.Tempo) && Near(LevelRun.EnemyShotSpeedScale, 1.035f * Difficulty.ShotSpeed), "First loop escalation is incorrect");
                            previousHealth = LevelRun.EnemyHealthScale;
                            previousTempo = LevelRun.EnemyTempoScale;
                            previousShots = LevelRun.EnemyShotSpeedScale;
                            LevelRun.GetChoices(out var left, out var right);
                            Check(left == LevelId.Snow && right == LevelId.City, "Plains must offer Snow/City");
                            Reject(LevelId.Fields);
                            Reject(LevelId.Thunder);
                            LevelRun.Advance(middle);
                            LevelRun.GetChoices(out left, out right);
                            Check(middle == LevelId.Snow ? left == LevelId.Desert && right == LevelId.Thunder : left == LevelId.Tunnel && right == LevelId.Candyland, "Middle tier must preserve its diagram branch");
                            Reject(LevelId.Fields);
                            Reject(middle);
                            LevelRun.Advance(third);
                            LevelRun.GetChoices(out left, out right);
                            var options = LevelRun.GetOptions();
                            Check(left == LevelId.Space && right == LevelId.Underwater && options.Length == 2 && options[0] == left && options[1] == right, "Third district must offer Space/Underwater");
                            Reject(LevelId.Fields);
                            Reject(LevelId.Snow);
                            LevelRun.Advance(extension);
                            Check(LevelRun.AutomaticNext == LevelId.Hell && !LevelRun.ReturnsToPlains && LevelRun.GetOptions().Length == 1, "Fourth tier must lead only into Hell");
                            Reject(LevelId.Fields);
                            Reject(extension);
                            LevelRun.Advance(LevelId.Hell);
                            Check(LevelRun.ReturnsToPlains && LevelRun.Stage == loop * 5, "Hell must finish the loop");
                            LevelRun.Advance(LevelId.Fields);
                            Check(LevelRun.Seed == seed && LevelRun.Loop == loop + 1, "Completed loop lost the seed or incremented incorrectly");
                        }
                    }
        }
        finally { LevelRun.Reset(); Difficulty.Select(originalDifficulty); }
        Check(LevelRun.Stage == 1 && LevelRun.Loop == 1 && Near(LevelRun.EnemyHealthScale, Difficulty.Health), "New run did not reset difficulty");
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < .0001f;
    static void Reject(LevelId next)
    {
        var current = LevelRun.Current;
        int stage = LevelRun.Stage;
        bool rejected = false;
        try
        {
            LevelRun.Advance(next);
        }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected && LevelRun.Current == current && LevelRun.Stage == stage, "Invalid route mutated the run");
    }

    static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
    }
}
