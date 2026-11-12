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
            foreach (var third in new[] { LevelId.Thunder, LevelId.Desert, LevelId.Tunnel, LevelId.Candyland })
            {
                const int seed = 17381;
                bool upper = third == LevelId.Thunder || third == LevelId.Desert;
                var middle = upper ? LevelId.Snow : LevelId.City;
                var fourth = upper ? LevelId.Space : LevelId.Underwater;
                var fifth = upper ? LevelId.Heaven : LevelId.Hell;
                LevelRun.Reset(LevelId.Fields, seed);
                CheckOptions(LevelId.Snow, LevelId.City);
                LevelRun.Advance(middle);
                if (upper) CheckOptions(LevelId.Thunder, LevelId.Desert);
                else CheckOptions(LevelId.Tunnel, LevelId.Candyland);
                LevelRun.Advance(third);
                CheckOptions(fourth);
                LevelRun.Advance(fourth);
                CheckOptions(fifth);
                LevelRun.Advance(fifth);
                CheckOptions(LevelId.Void);
                LevelRun.Advance(LevelId.Void);
                CheckOptions();
                Check(LevelRun.FinalDistrict && LevelRun.Stage == 6 && LevelRun.Loop == 1 && LevelRun.Seed == seed, "Void must finish the campaign with the same seed");
            }

            float previousHealth = 0, previousTempo = 0, previousShots = 0;
            for (int loop = 1; loop <= 50; loop++)
            {
                LevelRun.Preview(LevelId.Fields, 17381, loop);
                Check(LevelRun.Loop == loop && LevelRun.Stage == (loop - 1) * 6 + 1, "Preview loop or stage advanced incorrectly");
                Check(LevelRun.EnemyHealthScale > previousHealth && LevelRun.EnemyTempoScale >= previousTempo && LevelRun.EnemyShotSpeedScale >= previousShots, "Preview difficulty decreased");
                Check(LevelRun.EnemyTempoScale <= 1.3f * Difficulty.Tempo + .0001f && LevelRun.EnemyShotSpeedScale <= 1.18f * Difficulty.ShotSpeed + .0001f, "Readability speed caps exceeded");
                previousHealth = LevelRun.EnemyHealthScale;
                previousTempo = LevelRun.EnemyTempoScale;
                previousShots = LevelRun.EnemyShotSpeedScale;
            }
        }
        finally { LevelRun.Reset(); Difficulty.Select(originalDifficulty); }
        Check(LevelRun.Stage == 1 && LevelRun.Loop == 1 && Near(LevelRun.EnemyHealthScale, Difficulty.Health), "New run did not reset difficulty");
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < .0001f;

    static void CheckOptions(params LevelId[] expected)
    {
        var options = LevelRun.GetOptions();
        Check(options.Length == expected.Length, "Wrong number of exits from " + LevelRun.Current);
        for (int i = 0; i < options.Length; i++)
            Check(options[i] == expected[i], "Wrong exit from " + LevelRun.Current);
        Check(LevelRun.AutomaticNext == (expected.Length == 1 ? expected[0] : (LevelId?)null), "Wrong automatic handoff from " + LevelRun.Current);
        foreach (LevelId district in Enum.GetValues(typeof(LevelId)))
            if (Array.IndexOf(expected, district) < 0)
                Reject(district);
    }
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
