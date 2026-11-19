#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;

[InitializeOnLoad]
public static class ReviewTunnelRings
{
    public const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/TunnelRingsReview";
    static ReviewTunnelRings()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ReviewTunnelRings", false)) return;
            MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/TunnelRings/Save");
            new GameObject("Tunnel ring review").AddComponent<TunnelRingsReviewRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
    }
    public static void Run()
    {
        Guard();
        PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "TunnelRingsReview";
        MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/TunnelRings/Save");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ReviewTunnelRings", true); EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        Guard(); PlayerSettings.companyName = "DefaultCompany"; PlayerSettings.productName = "seup";
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = "Build/VoidBackdrop/Shootemup.exe", options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", result.summary.result + "\nErrors=" + result.summary.totalErrors + "; warnings=" + result.summary.totalWarnings);
        EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}

public sealed class TunnelRingsReviewRuntime : MonoBehaviour
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> report = new(), errors = new();
    void Check(bool okay, string message) { report.Add((okay ? "PASS " : "FAIL ") + message); if (!okay) errors.Add(message); }
    IEnumerator Ready() { yield return null; while (CloudTransition.Busy || !WavePreparation.Ready) yield return null; }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); AudioListener.pause = true; Time.captureDeltaTime = 1f / 60;
        Application.logMessageReceived += (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); };
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        var watcher = WaveWatcher.Current;
        if (watcher.Looping) watcher.ToggleLoop();
        watcher.SelectView(WatcherView.Bosses); yield return Ready();
        watcher.SelectLevel(LevelId.Tunnel); yield return Ready();
        watcher.SelectEncounter(WaveWatcher.BossOne); yield return Ready();
        foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
        {
            watcher.SelectDifficulty(difficulty); yield return Ready();
            TunnelSawBoss boss = null;
            for (int guard = 0; guard < 1200 && (boss == null || boss.Invulnerable); guard++)
            { boss = FindAnyObjectByType<TunnelSawBoss>(); yield return null; }
            Check(boss != null && boss.CurrentPhase == 1, difficulty + " enters phase one.");
            int created = CombatRound.CreatedCount, peak = 0, releases = 0, lastCount = 0;
            float worstTurn = 0, widest = 0;
            var samples = new List<double>(1200);
            using var frameTimer = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 1);
            using var physicsTimer = ProfilerRecorder.StartNew(ProfilerCategory.Physics2D, "Physics2D.Simulate", 1,
                ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
            double physicsPeak = 0;
            for (int frame = 0; frame < 1200; frame++)
            {
                yield return null;
                int orbitCount = 0;
                foreach (var round in CombatRound.Live)
                {
                    if (!(round is SimpleRound shot) || !shot.Orbiting) continue;
                    orbitCount++;
                    // Inspect a sparse sample outside the recorded frame timing.
                    if (frame % 60 != 0) continue;
                    float age = (float)typeof(CombatRound).GetField("age", Hidden).GetValue(shot);
                    var turn = (Func<float, float>)Delegate.CreateDelegate(typeof(Func<float, float>), shot,
                        typeof(SimpleRound).GetMethod("OrbitTurn", Hidden));
                    float radius = shot.OrbitRadius + age * 1.3f;
                    worstTurn = Mathf.Max(worstTurn, Mathf.Abs(turn(age + .01f) - turn(age)) / .01f * Mathf.Deg2Rad * radius);
                    widest = Mathf.Max(widest, radius);
                }
                if (orbitCount - lastCount >= Difficulty.Dense(36, LevelRun.BossPressure)) releases++;
                lastCount = orbitCount; peak = Mathf.Max(peak, CombatRound.Live.Count);
                if (frame % 60 != 1) samples.Add(frameTimer.LastValue / 1000000.0);
                physicsPeak = Math.Max(physicsPeak, physicsTimer.LastValue / 1000000.0);
            }
            samples.Sort();
            Check(releases >= 6 && widest > 7 && worstTurn <= 2.81f,
                $"{difficulty}: {releases} complete ring releases, radius up to {widest:F2}, tangential speed <= {worstTurn:F3}.");
            Check(CombatRound.CreatedCount == created, difficulty + " performs no projectile construction during phase one.");
            report.Add($"{difficulty}: {Difficulty.Dense(36, LevelRun.BossPressure)} teeth per complete ring; peak rounds={peak}; full CPU frame p95={samples[(int)(samples.Count*.95)]:F3}ms, max={samples.Last():F3}ms, physics max={physicsPeak:F3}ms. Rendering excluded.");
        }
        var probe = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down, 0);
        var release = typeof(CombatRound).GetMethod("Release", Hidden);
        var orbitAt = (Func<float, Vector2>)Delegate.CreateDelegate(typeof(Func<float, Vector2>), probe,
            typeof(SimpleRound).GetMethod("OrbitAt", Hidden));
        bool mirrored = true, bounded = true;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            probe.Orbit(1.4f, 0, sign * 30, 10, growth: .05f).Expand(1.3f, 2.8f);
            for (float t = .2f; t < 10; t += .1f)
            {
                Vector2 a = orbitAt(t), b = orbitAt(t + .01f);
                bounded &= Vector2.Distance(a, b) / .01f < 3.10f;
                mirrored &= Mathf.Sign(Vector2.SignedAngle(a, b)) == sign;
            }
        }
        Check(bounded && mirrored, "Both rotation directions stay within 3.10 units/s combined radial/tangential speed after the initial launch.");
        release.Invoke(probe, null);
        var reused = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down, 0);
        Check(ReferenceEquals(probe, reused), "Motion regression reuses the same pooled projectile.");
        reused.Orbit(1.4f, 17, 48, 10, .4f, 20, -25, .7f).Expand(1.3f);
        bool unchanged = true;
        for (float t = 0; t < 10; t += .1f)
            unchanged &= Vector2.Distance(orbitAt(t), SimpleRound.OrbitOffset(t, 1.4f + 1.3f*t, 17, 48, .4f, 20, -25, .7f)) < .0001f;
        Check(unchanged, "Other orbit programs retain their exact original trajectory, with no speed cap inherited from the pool.");
        release.Invoke(reused, null);
        report.AddRange(errors); File.WriteAllLines(ReviewTunnelRings.Folder + "/Checks.txt", report);
        SessionState.SetBool("ReviewTunnelRings", false); EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
}
#endif
