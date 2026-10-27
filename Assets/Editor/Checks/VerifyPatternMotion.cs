#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class VerifyPatternMotion
{
    static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/CombatReworkReview/")) throw new Exception("Use the isolated review copy");
        var before = JsonUtility.FromJson<CaptureDifficultyBaseline.Snapshot>(File.ReadAllText(CaptureDifficultyBaseline.File));
        Check(JsonUtility.ToJson(before) == JsonUtility.ToJson(CaptureDifficultyBaseline.Read(RunDifficulty.Easy)), "Easy parity changed");
        for (int frame = 0; frame <= 900; frame++)
        {
            var p = SimpleRound.OrbitOffset(frame / 60f, 2.25f, 0, 24, .74f, 0, -78, 1.6f);
            Check(float.IsFinite(p.x) && float.IsFinite(p.y) && p.magnitude < 3.0001f, "Orbital deployment exceeded its bounded geometry");
        }
        Check(SimpleRound.OrbitOffset(0, 2.25f, 0, 24, .74f, 0, -78, 1.6f) == Vector2.zero, "Orbit teleports away from its visible source");
        Check(Vector2.Distance(SimpleRound.OrbitOffset(3, 2.25f, 0, 24, .74f, 0, -78, 1.6f),
            SimpleRound.OrbitOffset(18, 2.25f, 0, 24, .74f, 0, -78, 1.6f)) > .5f, "The local ring is rigidly attached to the larger orbit");
        GameRules.StartNewRun(RunDifficulty.Hard); LevelRun.Reset(LevelId.Underwater, 17381);
        var sub = (AbyssSubmarineBoss)DistrictEnemy.Spawn(typeof(AbyssSubmarineBoss), new Vector3(0, 3, 1), true, true, false);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(DistrictBoss).GetField("phaseTwo", flags).SetValue(sub, true);
        typeof(AbyssSubmarineBoss).GetField("<EmergenceBursts>k__BackingField", flags).SetValue(sub, 2);
        typeof(AbyssSubmarineBoss).GetField("eruptionA", flags).SetValue(sub, true);
        typeof(AbyssSubmarineBoss).GetMethod("HardCrown", flags).Invoke(sub, new object[] { 0f, 2.2f });
        Check(CombatRound.Live.Count == 8 && CombatRound.Live.OfType<SimpleRound>().All(r => r.Trailing), "A mid-eruption phase change must restart with eight pressure fronts");
        CombatRound.ClearAll();
        var huge = CombatRound.Launch<SimpleRound>(new Vector3(0, 3), Vector2.down, 1.5f, .94f);
        Check(!huge.Orbiting && !huge.Trailing && !huge.ChargingSeed && !huge.Curving, "Pooled delivery state leaked into the next ordinary round");
        var target = new GameObject("Native pixel contact check");
        var hit = target.AddComponent<CircleCollider2D>(); hit.radius = .08f;
        target.transform.position = new Vector3(0, 3.7f); Physics2D.SyncTransforms();
        Check(huge.PixelOverlaps(hit), "A giant bullet's visible opaque body must hit");
        target.transform.position = new Vector3(1.2f, 4.2f); Physics2D.SyncTransforms();
        Check(!huge.PixelOverlaps(hit), "A giant bullet's transparent corner must remain safe");
        CombatRound.ClearAll();
        // Edit-mode automation does not dispatch MonoBehaviour.OnDisable. Check
        // the real clearing effect; Play mode owns the Live-registry callback.
        Check(CombatRound.Live.All(round => !round.gameObject.activeSelf), "Clearing leaves new deliveries active");
        CombatRound.Live.Clear();
        UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(sub.gameObject);
        File.WriteAllText("ArtSource/HardPatternsReview/MotionChecks.txt", "PASS\nFormer Hard/Easy parity retained; bounded double orbits deploy from the visible source and rotate locally; mid-eruption phase restart restores eight pressure fronts; pooled motion/delivery state resets; giant opaque pixels hit and transparent corners remain safe; clearing removes deliveries.\n");
        EditorApplication.Exit(0);
    }
}
#endif
