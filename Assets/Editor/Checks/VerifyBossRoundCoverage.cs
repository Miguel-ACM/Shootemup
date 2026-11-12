#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fixed-player native trajectory checks, deliberately independent of pattern implementation.
[InitializeOnLoad]
public static class VerifyBossRoundCoverage
{
    public const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/BossRoundCoverage";
    static double deadline;
    static VerifyBossRoundCoverage()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BossRoundCoverage", false)) return;
            deadline = EditorApplication.timeSinceStartup + 3600;
            new GameObject("Stationary boss coverage review").AddComponent<BossRoundCoverageRuntime>();
        };
        EditorApplication.update += () =>
        {
            if (deadline > 0 && EditorApplication.timeSinceStartup > deadline)
            { File.WriteAllText(Folder + "/Runtime.txt", "FAIL timeout"); EditorApplication.Exit(1); }
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Use the isolated command-line checkout");
    }
    public static void Run()
    {
        Guard(); Directory.CreateDirectory(Folder);
        PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "BossRoundCoverage";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("BossRoundCoverage", true); EditorApplication.isPlaying = true;
    }
    public static void RunFixes() { SessionState.SetBool("BossRoundCoverageFixes", true); Run(); }
    public static void Build()
    {
        Guard();
        if (!File.ReadAllText(Folder + "/Runtime.txt").StartsWith("PASS")) throw new Exception("Review must pass");
        PlayerSettings.companyName = "DefaultCompany"; PlayerSettings.productName = "seup";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "C:/Users/Miguel/Shootemup/Build/BossRoundCoverage/Shootemup.exe"
        });
        bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (ok ? "PASS" : "FAIL") + "\nErrors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        EditorApplication.Exit(ok ? 0 : 1);
    }
}

public sealed class BossRoundCoverageRuntime : MonoBehaviour
{
    readonly List<string> notes = new(), errors = new();
    readonly HashSet<CombatRound> previous = new();
    readonly StringBuilder results = new("case,point,x,y,seconds,contact\n");
    WaveWatcher watcher;
    Transform player;
    PolygonCollider2D probe;
    Vector3 target;
    bool testing;
    int peak, created, simple, needle;
    string current;
    void Log(string message, string stack, LogType kind)
    { if (kind == LogType.Error || kind == LogType.Exception) errors.Add(message + "\n" + stack); }
    void Note(string text)
    {
        notes.Add(text); File.WriteAllLines(VerifyBossRoundCoverage.Folder + "/Progress.txt", notes);
        Debug.Log("[Boss round coverage] " + text);
    }
    void Update()
    {
        if (!testing || player == null) return;
        player.position = target; probe.transform.position = target;
    }
    IEnumerator Ready()
    { yield return null; while (CloudTransition.Busy || !WavePreparation.Ready) yield return null; yield return null; }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); Application.logMessageReceived += Log;
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        watcher = WaveWatcher.Current; if (watcher.Looping) watcher.ToggleLoop();
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        Time.captureDeltaTime = 1f / 60;
        var ship = FindAnyObjectByType<Shipcontroller>(); player = ship.transform;
        var original = ship.GetComponent<PolygonCollider2D>();
        var go = new GameObject("Exact pilot probe"); go.transform.SetParent(transform);
        go.transform.localScale = player.lossyScale;
        probe = go.AddComponent<PolygonCollider2D>(); probe.points = original.points; probe.offset = original.offset;
        var body = go.AddComponent<Rigidbody2D>(); body.simulated = false;
        string filterPath = VerifyBossRoundCoverage.Folder + "/Cases.txt";
        var filter = SessionState.GetBool("BossRoundCoverageFixes", false) ? new HashSet<string>(File.ReadAllLines(filterPath)) : null;
        if (filter != null && File.Exists(VerifyBossRoundCoverage.Folder + "/Progress-Batch.txt"))
            notes.AddRange(File.ReadAllLines(VerifyBossRoundCoverage.Folder + "/Progress-Batch.txt")
                .Where(n => !filter.Any(f => n.Contains(f + " "))));
        if (filter != null && File.Exists(VerifyBossRoundCoverage.Folder + "/Targets-Batch.csv"))
            foreach (var line in File.ReadAllLines(VerifyBossRoundCoverage.Folder + "/Targets-Batch.csv").Skip(1))
                if (!filter.Contains(line.Split(',')[0])) results.AppendLine(line);
        foreach (var mode in new[] { RunDifficulty.Normal, RunDifficulty.Hard, RunDifficulty.Easy })
        {
            watcher.SelectDifficulty(mode); watcher.SelectView(WatcherView.Bosses); yield return Ready();
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                var types = DistrictWaves.Bosses(level);
                if (types.Count == 0) continue;
                if (filter != null && !types.Any(t => filter.Any(f => f.StartsWith(mode + "-" + t.Name + "-")))) continue;
                watcher.SelectLevel(level); yield return Ready();
                var entries = WatcherCatalog.For(WatcherView.Bosses, level);
                foreach (var entry in entries)
                {
                    if (entry.Encounter < WaveWatcher.BossOne) continue;
                    int phase = entry.Encounter == WaveWatcher.BossFour ? 4 : entry.Encounter == WaveWatcher.BossThree ? 3 : entry.Encounter == WaveWatcher.BossOne ? 1 : 2;
                    bool fast = entry.Encounter == WaveWatcher.BossOther;
                    if (mode == RunDifficulty.Easy && phase != 1 || mode == RunDifficulty.Hard && !fast && phase != (level == LevelId.Void ? 4 : 3)) continue;
                    current = mode + "-" + (entry.Enemy ?? types[0]).Name + "-" + phase + (fast ? "-Fast" : "");
                    bool include = filter == null || filter.Contains(current);
                    bool fastFinal = fast && mode == RunDifficulty.Hard && (filter == null || filter.Contains("Hard-CityDuoBoss-3-Fast"));
                    if (!include && !fastFinal) continue;
                    if (include) Note("START " + current + " native fixed-target audit");
                    watcher.SelectEntry(entries.IndexOf(entry)); yield return Ready();
                    DistrictBoss boss = null; float deadline = Time.time + 110;
                    while (Time.time < deadline)
                    {
                        boss = FindObjectsByType<DistrictBoss>().FirstOrDefault(b => b.GetType() == (entry.Enemy ?? types[0]) && b.health > 0 &&
                            b.CurrentPhase == phase && !b.Invulnerable && b.transform.position.y < Camera.UpLimit - 1 &&
                            (!(b is CityDuoBoss city) || phase == 1 || city.heavy != fast));
                        if (boss != null) break;
                        yield return null;
                    }
                    if (boss == null) { Note("FAIL " + current + " phase never ready"); continue; }
                    if (include) yield return Case();
                    // The catalog has a fast-survivor entry for phase two; also exercise its final score.
                    if (fastFinal)
                    {
                        current = mode + "-CityDuoBoss-3-Fast";
                        typeof(WaveWatcher).GetMethod("PushToPhase", BindingFlags.Static | BindingFlags.NonPublic)
                            .Invoke(null, new object[] { boss, null, 3 });
                        while (boss.CurrentPhase < 3 || boss.Invulnerable) yield return null;
                        Note("START " + current + " native fixed-target audit"); yield return Case();
                    }
                }
            }
        }
        testing = false; SessionState.SetBool("BossRoundCoverage", false); SessionState.SetBool("BossRoundCoverageFixes", false);
        bool okay = errors.Count == 0 && !notes.Any(n => n.StartsWith("FAIL"));
        File.WriteAllText(VerifyBossRoundCoverage.Folder + "/Runtime.txt", (okay ? "PASS" : "FAIL") +
            "\nNative headless simulation. Nine fixed target positions, exact approved sprite contacts and live beam footprints. No rendering.\n" +
            string.Join("\n", notes) + "\n" + string.Join("\n", errors));
        File.WriteAllText(VerifyBossRoundCoverage.Folder + "/Targets.csv", results.ToString());
        Application.logMessageReceived -= Log; EditorApplication.Exit(okay ? 0 : 1);
    }
    void Sample()
    {
        peak = Mathf.Max(peak, CombatRound.Live.Count);
        foreach (var shot in CombatRound.Live)
        {
            if (previous.Contains(shot)) continue;
            if (shot is SimpleRound) simple++;
            if (shot is NeedleRound) needle++;
        }
        previous.Clear(); foreach (var shot in CombatRound.Live) previous.Add(shot);
    }
    string Contact()
    {
        foreach (var shot in CombatRound.Live)
        {
            Vector2 delta = target - shot.transform.position;
            if (shot is PulseLaser beam)
            {
                if (beam.IsFiring && beam.Power > .15f && BeamContact.Touches(probe, beam.transform.position,
                    beam.transform.position + (Vector3)beam.Heading * beam.length,
                    beam.width * beam.Power * .5f * ProjectilePixelContact.HitboxScale, out _)) return "beam";
            }
            else if (shot.UsesPixelContact && delta.sqrMagnitude < 9 && shot.PixelOverlaps(probe)) return shot.GetType().Name;
        }
        return null;
    }
    IEnumerator Case()
    {
        peak = simple = needle = 0; created = CombatRound.CreatedCount; previous.Clear();
        float start = Time.time; int hits = 0;
        float[] xs = { Camera.LeftLimit + .35f, 0, Camera.RightLimit - .35f };
        float[] ys = { Camera.DownLimit + .35f, (Camera.DownLimit + Camera.UpLimit) * .5f, Camera.UpLimit - .35f };
        int point = 0; testing = true;
        foreach (float y in ys) foreach (float x in xs)
        {
            target = new Vector3(x, y, player.position.z); player.position = target; probe.transform.position = target;
            float began = Time.time; string contact = null;
            while (Time.time - began < 36)
            {
                yield return null; Sample();
                contact = Contact(); if (contact != null) break;
            }
            if (contact != null) hits++;
            results.AppendLine(FormattableString.Invariant($"{current},{point++},{x},{y},{Time.time - began:F2},{contact ?? "NONE"}"));
        }
        // Observe enough of the authored score to count both silhouette families.
        while (Time.time - start < 36) { yield return null; Sample(); }
        testing = false;
        bool okay = hits == 9 && simple > 0 && peak < CombatRound.Limit && CombatRound.CreatedCount == created;
        Note((okay ? "PASS " : "FAIL ") + current + " contacts=" + hits + "/9 simple=" + simple + " needle=" + needle +
            " peak=" + peak + " constructed=" + (CombatRound.CreatedCount - created));
        File.WriteAllText(VerifyBossRoundCoverage.Folder + "/Targets.csv", results.ToString());
    }
}
#endif
