#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CaptureDifficultyBaseline
{
    [Serializable]
    public sealed class Actor
    {
        public string name; public float health, interval;
    }

    [Serializable]
    public sealed class Phrase
    {
        public string level; public int budget; public string score;
    }

    [Serializable]
    public sealed class Snapshot
    {
        public float health, tempo, speed, damage; public List<Actor> actors = new List<Actor>(); public List<Phrase> waves = new List<Phrase>();
    }

    public const string File = "ArtSource/HardPatternsReview/FormerHard.json";
    static FieldInfo Interval = typeof(DistrictEnemy).GetField("attackInterval", BindingFlags.Instance | BindingFlags.NonPublic);
    public static Snapshot Read(RunDifficulty mode)
    {
        GameRules.StartNewRun(mode);
        var s = new Snapshot { health = Difficulty.Health, tempo = Difficulty.Tempo, speed = Difficulty.ShotSpeed, damage = Difficulty.Damage };
        foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            LevelRun.Reset(level, 17381);
            foreach (var type in WatcherCatalog.Enemies(level).Concat(new[] { DistrictWaves.BossType }).Distinct())
            {
                var actor = DistrictEnemy.Spawn(type, Vector3.zero, true, true, false);
                s.actors.Add(new Actor { name = level + "/" + type.Name, health = actor.health, interval = (float)Interval.GetValue(actor) });
                UnityEngine.Object.DestroyImmediate(actor.gameObject);
            }
            foreach (int budget in new[] { 2, 6, 12, 20 })
                s.waves.Add(new Phrase { level = level.ToString(), budget = budget, score = WaveVarietyAudit.Signature(DistrictWaves.Create(budget)) });
        }
        return s;
    }

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/CombatReworkReview/"))
            throw new Exception("Use the isolated review copy");
        Directory.CreateDirectory(Path.GetDirectoryName(File));
        System.IO.File.WriteAllText(File, JsonUtility.ToJson(Read(RunDifficulty.Hard), true));
        EditorApplication.Exit(0);
    }

    public static void VerifyEasy()
    {
        var before = JsonUtility.FromJson<Snapshot>(System.IO.File.ReadAllText(File));
        var after = Read(RunDifficulty.Easy);
        if (JsonUtility.ToJson(before) != JsonUtility.ToJson(after))
            throw new Exception("Easy differs from the recorded former Hard baseline");
        System.IO.File.WriteAllText("ArtSource/HardPatternsReview/EasyParity.txt", "PASS\nEasy exactly matches former Hard: global factors, " + after.actors.Count + " configured actors, and " + after.waves.Count + " deterministic production wave signatures.\n");
        EditorApplication.Exit(0);
    }
}
#endif
