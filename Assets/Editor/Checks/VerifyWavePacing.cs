using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;

// Runs as the WavePacing test in Unity's Test Runner (Assets/Editor/Tests/LogicTests.cs).
public static class VerifyWavePacing
{
    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    static FieldInfo Field(string name) => typeof(WaveOrchestrer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    static void Set(WaveOrchestrer o, string name, object value) => Field(name).SetValue(o, value);
    public static void Run()
    {
        var go = new GameObject("Wave pacing verification");
        try
        {
            var pool = go.AddComponent<WavePool>();
            for (int trial = 0; trial < 1000; trial++)
            {
                int budget = 1 + trial % 5;
                var wave = pool.getWave(budget.ToString());
                Check(wave.cost > 0 && wave.cost <= budget, "Wave budget exceeded or empty");
                float previous = -1;
                foreach (var unit in wave.units)
                {
                    Check(unit.time >= previous && unit.time <= wave.duration, "Spawn timing is unordered or outside duration");
                    Check(unit.GetUnit() != null, "Missing enemy prefab: " + unit.enemy);
                    previous = unit.time;
                }
                var clone = (Wave)wave.Clone();
                var first = clone.getNext();
                Check(first != null && !ReferenceEquals(first, wave.units[0]), "Wave clone shares spawn state");
            }
            Check(pool.getWave("100").cost <= 100, "Exhausted conflict budget failed");
            var formations = new List<Wave>();
            for (int i = 0; i < 3; i++)
                formations.Add(new Wave(1, 5, 1, 0, new List<WaveUnit> { new WaveUnit("enemy1", 0, Vector3.zero) }));
            for (int i = 0; i < 100; i++)
            {
                var combined = Wave.CombineWaves(formations);
                Check(combined.units[1].time >= .9f && combined.units[1].time <= 1.4f, "First support gap");
                float gap = combined.units[2].time - combined.units[1].time;
                Check(gap >= .899f && gap <= 1.401f, "Second support gap");
            }
            var director = go.AddComponent<WaveOrchestrer>();
            var pending = new WaveUnit("enemy1", 8, Vector3.zero);
            Set(director, "currentWave", new Wave(1, 10, 1, 0, new List<WaveUnit> { pending }));
            Set(director, "currentUnit", pending);
            Set(director, "currentWaveNumber", 1);
            Set(director, "currentWaveEnemies", 1);
            Set(director, "waveTime", 1f);
            director.NotifyEnemyKilled(1);
            Check((float)Field("waveTime").GetValue(director) == 1f, "Clear skipped a pending reinforcement");
            Set(director, "wavePool", pool);
            Set(director, "currentUnit", null);
            Set(director, "isWaveComingOut", true);
            Set(director, "clearTime", .7f);
            director.waves = new[] { "1", "1" };
            typeof(WaveOrchestrer).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
            Check((int)Field("currentWaveNumber").GetValue(director) == 2, "Cleared wave did not advance after breather");
            Check((float)Field("waveTime").GetValue(director) == 0f, "Clear jump carried phantom time");
            Debug.Log("WAVE PACING PASS: 1000 budget/prefab/order checks; stagger bounds; clone isolation; conflict exhaustion; reinforcement and clear transition checks.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
